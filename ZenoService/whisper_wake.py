"""Local whisper.cpp speech recognition for Zeno's wake phrase."""
import os
from pathlib import Path
import subprocess
import tempfile
import wave


class WhisperWake:
    def __init__(self, directory):
        directory = Path(directory)
        programs = list(directory.rglob('whisper-cli.exe'))
        self.program = programs[0] if programs else None
        self.model = directory / 'ggml-tiny.en.bin'

    @property
    def ready(self):
        return bool(self.program and self.model.is_file())

    def transcribe(self, pcm):
        if not self.ready:
            raise RuntimeError('Whisper wake model is not installed.')
        with tempfile.TemporaryDirectory(prefix='zeno-whisper-') as folder:
            audio = Path(folder) / 'question.wav'
            with wave.open(str(audio), 'wb') as wav:
                wav.setnchannels(1); wav.setsampwidth(2); wav.setframerate(16000)
                wav.writeframes(pcm)
            result = subprocess.run([
                str(self.program), '--model', str(self.model), '--file', str(audio),
                '--language', 'en', '--no-prints', '--no-timestamps', '--no-gpu',
                '--prompt', 'Hey Zeno. What does this part do?'],
                stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True,
                encoding='utf-8', errors='replace', timeout=35,
                creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
            if result.returncode:
                raise RuntimeError('Whisper could not recognize the question.')
            return ' '.join(result.stdout.split())[:1500]
