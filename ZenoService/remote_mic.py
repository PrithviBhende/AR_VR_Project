"""Optional headset capture on the local Windows helper, never persisted."""
import array
import base64
import collections
import io
import math
import threading
import time
import wave

import sounddevice as sd


def find_input(name):
    if not name:
        index = sd.default.device[0]
        if index is None or index < 0:
            raise ValueError('Windows has no default recording device. Choose one in Sound settings.')
        device = sd.query_devices(index)
        if device['max_input_channels'] < 1:
            raise ValueError('Windows default device has no microphone input.')
        return index, device['name']
    options = []
    for index, device in enumerate(sd.query_devices()):
        if device['max_input_channels'] < 1 or name.lower() not in device['name'].lower():
            continue
        host = sd.query_hostapis(device['hostapi'])['name']
        options.append((0 if 'WASAPI' in host else 1 if 'MME' in host else 2, index, device['name']))
    if not options:
        raise ValueError('The selected headset microphone is not available in Windows.')
    return min(options)[1:]


class RemoteMic:
    def __init__(self, service):
        self.service = service
        self.lock = threading.Lock()
        self.thread = None
        self.active = False
        self.playback = False
        self.events = collections.deque(maxlen=2)
        self.context = ('', 'inspection')
        self.last_poll = 0
        self.force = False
        self.armed = False
        self.level = 0
        self.error = ''
        self.device_name = ''
        self.session = ''
        self.generation = 0

    def start(self, device_name, session):
        self.stop()
        index, display = find_input(device_name)
        # The WH-1000XM5 hands-free input reports a native 16 kHz stream.
        sd.check_input_settings(device=index, channels=1, samplerate=16000, dtype='int16')
        with self.lock:
            self.active = True; self.last_poll = time.monotonic()
            self.playback = self.force = self.armed = False
            self.context = ('', 'inspection'); self.level = 0; self.error = ''
            self.events.clear(); self.device_name = display; self.session = session
        generation = self.generation
        self.thread = threading.Thread(target=self.run, args=(index, generation, not device_name), daemon=True)
        self.thread.start()
        return {'kind': 'listening', 'message': display}

    def stop(self):
        with self.lock:
            self.active = False; self.events.clear(); self.level = 0
            self.generation += 1
        # Never wait for an in-flight Gemini request on the Unity thread.

    def poll(self, part_id, mode, session):
        with self.lock:
            if not self.active or session != self.session:
                return {'kind': 'stopped', 'error': self.error or 'Headset capture stopped.'}
            self.last_poll = time.monotonic()
            self.context = (part_id, mode)
            if self.events:
                return self.events.popleft()
            return {'kind': 'listening', 'level': self.level, 'message': self.device_name}

    def command(self, action, session):
        with self.lock:
            if not self.active or session != self.session:
                return {'kind': 'stopped'}
            self.last_poll = time.monotonic()
            if action == 'ask': self.force = True
            if action == 'resume': self.playback = False
            return {'kind': 'listening'}

    def run(self, index, generation, follow_default):
        try:
            with sd.RawInputStream(device=index, samplerate=16000, channels=1, dtype='int16') as stream:
                pre = collections.deque(maxlen=50)
                speech = []
                voiced = silent = 0
                speaking = False
                last_default_check = time.monotonic()
                while True:
                    with self.lock:
                        if not self.active or generation != self.generation or time.monotonic() - self.last_poll > 4:
                            self.active = False; return
                        playback = self.playback
                    if follow_default and time.monotonic() - last_default_check > 2:
                        last_default_check = time.monotonic()
                        if sd.default.device[0] != index:
                            with self.lock:
                                self.error = 'Windows default microphone changed. Reconnecting.'
                                self.active = False
                            return
                    if playback:
                        time.sleep(.03); continue
                    chunk, overflow = stream.read(320)
                    samples = array.array('h'); samples.frombytes(chunk)
                    level = math.sqrt(sum(value * value for value in samples) / len(samples)) / 32768
                    with self.lock: self.level = level
                    loud = level >= .001
                    if not speaking and loud:
                        speaking = True; speech = list(pre); pre.clear(); silent = voiced = 0
                        with self.lock: captured = self.context
                    if speaking:
                        speech.append(chunk)
                        silent = 0 if loud else silent + 320
                        if loud: voiced += 320
                        if silent >= 19200 or len(speech) >= 700:
                            utterance = b''.join(speech) if voiced >= 2880 else b''
                            speaking = False; speech = []; pre.clear()
                            if utterance: self.answer(utterance, captured, generation)
                    else:
                        pre.append(chunk)
        except Exception as error:
            with self.lock:
                self.error = 'Microphone capture failed: ' + type(error).__name__
                if generation == self.generation: self.active = False

    def answer(self, pcm, context, generation):
        with self.lock:
            if not self.active or generation != self.generation: return
            action = 'push' if self.force else 'question' if self.armed else 'wake'
            self.force = False
            self.playback = True
            session = self.session
        stream = io.BytesIO()
        with wave.open(stream, 'wb') as wav:
            wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(16000); wav.writeframes(pcm)
        data = {'session': session, 'action': action, 'part_id': context[0], 'mode': context[1],
                'audio': base64.b64encode(stream.getvalue()).decode()}
        try:
            with self.service.lock:
                response = self.service.handle(data)
        except Exception as error:
            response = {'kind': 'error', 'message': str(error) if error.__class__.__name__ == 'ServiceError' else 'Could not answer. Try again.'}
        with self.lock:
            if not self.active or generation != self.generation or session != self.session: return
            if response['kind'] == 'wake': self.armed = True
            elif response['kind'] in ('answer', 'expired'): self.armed = False
            if response['kind'] in ('ignored', 'wake', 'expired'):
                self.playback = False
            self.events.append(response)
