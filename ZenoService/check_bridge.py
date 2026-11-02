"""Exercise headset capture locally; no recording is saved or sent to Gemini."""
import json
from pathlib import Path
import time
import urllib.request
import urllib.error
import uuid

settings = json.loads((Path(__file__).parent / '.local/settings.json').read_text(encoding='utf-8-sig'))
session = uuid.uuid4().hex

def send(action):
    request = urllib.request.Request('http://127.0.0.1:8765/mic',
        data=json.dumps({'action': action, 'session': session, 'device': 'WH-1000XM5'}).encode(),
        headers={'Content-Type': 'application/json', 'X-Zeno-Token': settings['client_token']})
    try:
        with urllib.request.urlopen(request, timeout=8) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        print('Helper error:', error.read().decode(), flush=True)
        raise

try:
    started = send('start')
    print('Start:', started.get('kind'), started.get('message'))
    for _ in range(5):
        time.sleep(.5)
        response = send('poll')
        print('Poll:', response.get('kind'), 'level:', round(response.get('level', 0), 5))
finally:
    print('Stop:', send('stop').get('kind'))
