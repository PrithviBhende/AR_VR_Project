"""Install official whisper.cpp CPU release and tiny English model locally."""
from pathlib import Path
import shutil
import urllib.request
import zipfile

here = Path(__file__).resolve().parent
target = here / 'models' / 'whisper'
target.mkdir(parents=True, exist_ok=True)
release = 'https://github.com/ggml-org/whisper.cpp/releases/download/b5454/whisper-bin-x64.zip'
model = 'https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.en.bin'

def download(url, path, minimum):
    temporary = path.with_suffix(path.suffix + '.partial')
    try:
        with urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'Zeno-learning-project'}), timeout=45) as response:
            with temporary.open('wb') as output:
                shutil.copyfileobj(response, output)
        if temporary.stat().st_size < minimum:
            raise RuntimeError('Download was incomplete: ' + path.name)
        temporary.replace(path)
    finally:
        temporary.unlink(missing_ok=True)

archive = target / 'whisper-bin-x64.zip'
if not any(target.rglob('whisper-cli.exe')):
    print('Downloading official whisper.cpp Windows CPU build...', flush=True)
    download(release, archive, 1_000_000)
    with zipfile.ZipFile(archive) as zipped:
        for item in zipped.infolist():
            if not (target / item.filename).resolve().is_relative_to(target.resolve()):
                raise RuntimeError('Invalid release archive entry')
        zipped.extractall(target)
    archive.unlink()
weights = target / 'ggml-tiny.en.bin'
if not weights.is_file():
    print('Downloading official tiny English model...', flush=True)
    download(model, weights, 70_000_000)
print('Whisper installed:', next(target.rglob('whisper-cli.exe')).name, weights.stat().st_size, flush=True)
