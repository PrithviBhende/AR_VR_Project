"""Explicit live smoke test. Uses Gemini quota; never prints keys or tokens."""
import base64
import io
import json
import sys
from pathlib import Path
import urllib.error
import urllib.request
import uuid
import wave

settings = json.loads((Path(__file__).parent / '.local/settings.json').read_text(encoding='utf-8-sig'))
body = {'session': uuid.uuid4().hex, 'action': 'text', 'part_id': 'bonnet',
        'mode': 'inspection', 'text': 'What does this part do, and is the engine underneath it?'}
if '--audio' in sys.argv:
    from server import speak
    from audio_tools import pcm16k
    pcm = pcm16k(speak('Hey Zeno, why is this part important?'))
    stream = io.BytesIO()
    with wave.open(stream, 'wb') as recorded:
        recorded.setnchannels(1); recorded.setsampwidth(2); recorded.setframerate(16000)
        recorded.writeframes(pcm)
    body.update(action='wake', part_id='engine', text='', audio=base64.b64encode(stream.getvalue()).decode())
request = urllib.request.Request('http://127.0.0.1:8765/turn', data=json.dumps(body).encode(),
                                 headers={'Content-Type': 'application/json', 'X-Zeno-Token': settings['client_token']})
try:
    with urllib.request.urlopen(request, timeout=85) as response:
        result = json.load(response)
    print(json.dumps({k: v for k, v in result.items() if k != 'audio'}, ensure_ascii=True))
    if result.get('audio'):
        with wave.open(io.BytesIO(base64.b64decode(result['audio'])), 'rb') as audio:
            print('Reply audio seconds:', round(audio.getnframes() / audio.getframerate(), 2))
    else:
        raise SystemExit('No reply audio received.')
except urllib.error.HTTPError as error:
    print('Service error:', error.code, error.read().decode())
    raise SystemExit(1)
