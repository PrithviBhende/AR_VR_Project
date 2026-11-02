"""Deterministic tests: no API credentials, microphone, or paid requests."""
import base64
import io
import json
import threading
import unittest
import urllib.error
import urllib.request
import wave
from unittest.mock import patch
import server
from remote_mic import RemoteMic


def wav():
    stream = io.BytesIO()
    with wave.open(stream, 'wb') as out:
        out.setnchannels(1); out.setsampwidth(2); out.setframerate(16000)
        out.writeframes(b'\0\0' * 16000)
    return base64.b64encode(stream.getvalue()).decode()


class Wake:
    ready = True
    text = ''
    def transcribe(self, _): return self.text


class Provider:
    key = 'fake-test-key'
    model = 'test'
    calls = []
    def __init__(self): self.calls = []
    def answer(self, audio, text, context, history):
        self.calls.append((audio, text, context, list(history)))
        return text or 'what does this do', 'This is an educational test reply.'


class ZenoTests(unittest.TestCase):
    def setUp(self):
        self.provider, self.wake = Provider(), Wake()
        self.zeno = server.Zeno(self.provider, self.wake, lambda _: b'', [
            {'id': 'engine', 'explanation': 'Rear engine', 'hint': 'Behind the seats'},
            {'id': 'bonnet', 'explanation': 'Front bonnet', 'hint': 'At the front'}])
        self.request = {'session': 'a' * 32, 'part_id': 'engine', 'mode': 'exploded', 'action': 'wake', 'audio': wav()}

    def test_wake_pronunciations(self):
        for name in ('zeno', 'zeeno', 'zino', 'jeeno', 'zzno', 'zero', 'zee no', 'zen oh'):
            with self.subTest(name=name):
                self.assertEqual(server.strip_wake('hey ' + name + ' tell me more'), (True, 'tell me more'))

    def test_headset_observed_phonetic_spelling(self):
        self.assertFalse(server.strip_wake('easy know what does the engine do')[0])
        self.assertRegex('easy know what does the engine do', r'^(?:hazy know|hey know|easy know)\b')

    def test_background_does_not_trigger(self):
        for text in ('tell me more', 'this engine has zero oil', 'they said hey zeno', 'hello there', 'hey there'):
            self.assertFalse(server.strip_wake(text)[0])
            self.wake.text = text
            ignored = self.zeno.handle(self.request)
            self.assertEqual(ignored['kind'], 'ignored')
            self.assertEqual(ignored['transcript'], text)
        self.assertFalse(self.provider.calls)

    def test_wake_plus_question_is_one_request(self):
        self.wake.text = 'hey zeno what does this do'
        response = self.zeno.handle(self.request)
        self.assertEqual(response['kind'], 'answer')
        self.assertEqual(self.provider.calls[0][2]['part']['id'], 'engine')
        self.assertTrue(self.provider.calls[0][0])

    def test_wake_then_question(self):
        self.wake.text = 'hey zeno'
        self.assertEqual(self.zeno.handle(self.request)['kind'], 'wake')
        self.assertFalse(self.provider.calls)
        self.request['action'] = 'question'
        self.assertEqual(self.zeno.handle(self.request)['kind'], 'answer')
        self.assertEqual(self.zeno.handle(self.request)['kind'], 'expired')

    def test_followup_without_wake_is_rejected(self):
        self.request['action'] = 'question'
        self.assertEqual(self.zeno.handle(self.request)['kind'], 'expired')
        self.assertFalse(self.provider.calls)

    def test_expired_wake_is_rejected(self):
        self.wake.text = 'hey zeno'
        self.zeno.handle(self.request)
        self.zeno.sessions['a' * 32]['armed'] = 0
        self.request['action'] = 'question'
        self.assertEqual(self.zeno.handle(self.request)['kind'], 'expired')

    def test_push_to_ask_bypasses_wake_only(self):
        self.request['action'] = 'push'
        self.assertEqual(self.zeno.handle(self.request)['kind'], 'answer')
        self.assertEqual(len(self.provider.calls), 1)

    def test_unknown_context_is_not_invented(self):
        self.request.update(action='text', text='what is this', part_id='invented part')
        self.zeno.handle(self.request)
        self.assertIsNone(self.provider.calls[-1][2]['part'])

    def test_context_changes_override_previous_part(self):
        self.request.update(action='text', text='what is this')
        self.zeno.handle(self.request)
        self.request['part_id'] = 'bonnet'
        self.zeno.handle(self.request)
        self.assertEqual(self.provider.calls[-1][2]['part_id'], 'bonnet')
        self.assertEqual(self.provider.calls[-1][3][0]['part_id'], 'engine')

    def test_general_question_has_no_pointer_requirement(self):
        self.request.update(action='text', text='Compare the Enzo and F40', part_id='')
        self.assertEqual(self.zeno.handle(self.request)['kind'], 'answer')

    def test_history_bounded_and_reset(self):
        self.request.update(action='text', text='tell me a fact')
        for _ in range(6): self.zeno.handle(self.request)
        self.assertEqual(len(self.zeno.sessions['a' * 32]['history']), 4)
        self.request['action'] = 'reset'; self.zeno.handle(self.request)
        self.assertNotIn('a' * 32, self.zeno.sessions)

    def test_rate_limit(self):
        self.request.update(action='text', text='tell me a fact')
        for _ in range(8): self.zeno.handle(self.request)
        with self.assertRaises(server.ServiceError): self.zeno.handle(self.request)
        self.assertEqual(len(self.provider.calls), 8)

    def test_audio_validation(self):
        for invalid in ('not base64', base64.b64encode(b'not a WAV').decode()):
            with self.assertRaises(server.ServiceError): server.read_wav(invalid)
        self.assertEqual(len(server.read_wav(wav())[1]), 32000)

    def test_provider_errors_do_not_add_history(self):
        self.request.update(action='text', text='what is this')
        with patch.object(self.provider, 'answer', side_effect=server.ServiceError('offline', 502)):
            with self.assertRaises(server.ServiceError): self.zeno.handle(self.request)
        self.assertEqual(self.zeno.sessions['a' * 32]['history'], [])

    def test_speech_failure_keeps_answer(self):
        self.request.update(action='text', text='what is this')
        self.zeno.speaker = lambda _: (_ for _ in ()).throw(OSError())
        response = self.zeno.handle(self.request)
        self.assertEqual(response['kind'], 'answer')
        self.assertIn('Speech unavailable', response['message'])

    def test_gemini_request_contract(self):
        result = {'candidates': [{'content': {'parts': [{'text': json.dumps({'transcript': 'Why?', 'answer': 'For grip.'})}]}}]}
        class Response(io.BytesIO):
            def __enter__(self): return self
            def __exit__(self, *_): self.close()
        with patch('urllib.request.urlopen', return_value=Response(json.dumps(result).encode())) as call:
            gemini = server.Gemini('test-secret')
            transcript, answer = gemini.answer(server.read_wav(wav())[0], '', {'part_id': 'wheel_FL'}, [])
            request = call.call_args.args[0]
            self.assertNotIn('test-secret', request.full_url)
            body = json.loads(request.data)
            self.assertIn('inlineData', body['contents'][0]['parts'][1])
            self.assertEqual((transcript, answer), ('Why?', 'For grip.'))

    def test_gemini_missing_key(self):
        with self.assertRaises(server.ServiceError): server.Gemini('').answer(b'', 'question', {}, [])

    def test_headset_utterance_uses_captured_part(self):
        remote = RemoteMic(self.zeno)
        remote.active = True; remote.session = 'a' * 32
        self.wake.text = 'hey zeno what does this part do'
        remote.answer(server.read_wav(wav())[1], ('engine', 'inspection'), remote.generation)
        event = remote.events.popleft()
        self.assertEqual(event['kind'], 'answer')
        self.assertEqual(event['part_id'], 'engine')
        self.assertTrue(remote.playback)
        remote.stop()

    def test_headset_stop_discards_delayed_answer(self):
        remote = RemoteMic(self.zeno)
        remote.active = True; remote.session = 'a' * 32
        self.wake.text = 'hey zeno what does this part do'
        generation = remote.generation
        def stopped(*args):
            remote.stop()
            return ('question', 'late reply')
        self.provider.answer = stopped
        remote.answer(server.read_wav(wav())[1], ('bonnet', 'inspection'), generation)
        self.assertFalse(remote.events)

    def test_http_auth_and_health(self):
        http = server.ThreadingHTTPServer(('127.0.0.1', 0), server.Handler)
        http.token = 'test-token'; http.zeno = self.zeno
        worker = threading.Thread(target=http.serve_forever, daemon=True); worker.start()
        url = f'http://127.0.0.1:{http.server_port}/health'
        try:
            with self.assertRaises(urllib.error.HTTPError) as error: urllib.request.urlopen(url)
            self.assertEqual(error.exception.code, 401)
            request = urllib.request.Request(url, headers={'X-Zeno-Token': 'test-token'})
            with urllib.request.urlopen(request) as response: self.assertTrue(json.load(response)['ready'])
            request.add_header('Origin', 'https://example.org')
            with self.assertRaises(urllib.error.HTTPError): urllib.request.urlopen(request)
        finally:
            http.shutdown(); http.server_close(); worker.join()


if __name__ == '__main__':
    unittest.main()
