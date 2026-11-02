"""Convert Windows speech test WAVs to mono 16 kHz PCM."""
import array
import io
import wave

def pcm16k(wav_bytes):
    with wave.open(io.BytesIO(wav_bytes), 'rb') as audio:
        assert audio.getsampwidth() == 2
        channels, rate = audio.getnchannels(), audio.getframerate()
        samples = array.array('h', audio.readframes(audio.getnframes()))
    mono = [sum(samples[i:i + channels]) / channels for i in range(0, len(samples), channels)]
    output = array.array('h')
    for index in range(int(len(mono) * 16000 / rate)):
        position = index * rate / 16000
        left = int(position); right = min(left + 1, len(mono) - 1)
        output.append(round(mono[left] + (mono[right] - mono[left]) * (position - left)))
    return output.tobytes()
