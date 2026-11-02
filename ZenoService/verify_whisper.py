"""Generated-voice wake test with the installed local Whisper model."""
from audio_tools import pcm16k
from server import HERE, speak, strip_wake
from whisper_wake import WhisperWake

wake = WhisperWake(HERE / 'models/whisper')
assert wake.ready
cases = [('Hey Zeno, what does this part do?', True),
         ('Hey Zeeno, tell me more about this engine.', True),
         ('Hey Zino, why is this part important?', True),
         ('Hey Jeeno, tell me about the Enzo.', True),
         ('What does the engine do?', False)]
failed = 0
for phrase, wanted in cases:
    heard = wake.transcribe(pcm16k(speak(phrase)))
    detected, _ = strip_wake(heard)
    ok = detected == wanted
    failed += not ok
    print(f'{"PASS" if ok else "FAIL"} {phrase} -> {heard}', flush=True)
raise SystemExit(1 if failed else 0)
