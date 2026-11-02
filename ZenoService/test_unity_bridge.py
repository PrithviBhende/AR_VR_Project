"""Local fixture for Unity's network check. Never calls Gemini or records a mic.
Run explicitly during tests only. The real service continues to use port 8765.
"""
import json
import time
from server import HERE, Handler, ThreadingHTTPServer, Zeno

class FixtureProvider:
    key = 'fixture-only'
    model = 'test-fixture'
    def answer(self, audio, text, context, history):
        time.sleep(1)
        return text, f'Test reply for {context["part_id"] or "whole car"}. Previous turns: {len(history)}. This is a local test, not a Gemini answer.'

class NoWake:
    ready = False

if __name__ == '__main__':
    config = json.loads((HERE / '.local/settings.json').read_text(encoding='utf-8-sig'))
    http = ThreadingHTTPServer(('127.0.0.1', 8766), Handler)
    http.token = config['client_token']
    http.zeno = Zeno(FixtureProvider(), NoWake())
    print('Local test fixture on 8766. No Gemini calls.', flush=True)
    try: http.serve_forever()
    except KeyboardInterrupt: pass
    finally: http.server_close()
