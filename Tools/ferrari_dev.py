"""Run bounded commands through the existing Unity editor relay, without changing source evidence."""
import json, subprocess, threading, queue, time, sys
from pathlib import Path
root = Path(__file__).resolve().parents[1]
proc = subprocess.Popen([str(Path.home()/'.unity/relay/relay_win.exe'), '--mcp'], stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, encoding='utf-8', errors='replace')
messages = queue.Queue()
def read():
    for line in proc.stdout:
        try: messages.put(json.loads(line))
        except ValueError: pass
threading.Thread(target=read, daemon=True).start()
def request(i, method, params):
    proc.stdin.write(json.dumps(dict(jsonrpc='2.0', id=i, method=method, params=params))+'\n'); proc.stdin.flush()
    deadline = time.monotonic()+50
    while time.monotonic()<deadline:
        try: result=messages.get(timeout=1)
        except queue.Empty: continue
        if result.get('id')==i: return result
    raise TimeoutError(method)
try:
    request(1, 'initialize', dict(protocolVersion='2024-11-05', capabilities={}, clientInfo=dict(name='FerrariDevelopment', version='1')))
    proc.stdin.write(json.dumps(dict(jsonrpc='2.0',method='notifications/initialized'))+'\n'); proc.stdin.flush()
    if sys.argv[1]=='console':
        params=dict(name='Unity_GetConsoleLogs', arguments=dict(maxEntries=12, includeStackTrace=True, logTypes='Error'))
    else:
        code=Path(sys.argv[1]).read_text(encoding='utf-8') if sys.argv[1].endswith('.cs') else sys.argv[1]
        params=dict(name='Unity_RunCommand', arguments=dict(Code='using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; internal class CommandScript : IRunCommand { public void Execute(ExecutionResult result) { '+code+' } }', Title='Ferrari development'))
    result=request(2,'tools/call',params)
    output=result.get('result', {}).get('structuredContent')
    if output:
        data=output.get('data', {})
        print(json.dumps(dict(success=output.get('success'), message=output.get('message'), compilation=data.get('compilationLogs'), logs=data.get('executionLogs'), data={k:v for k,v in data.items() if k not in ('localFixedCode','compilationLogs','executionLogs')}), ensure_ascii=False))
    else: print(json.dumps(result, ensure_ascii=False))
finally:
    proc.terminate()
