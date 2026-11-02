"""Check Windows microphone signal levels without saving or sending audio."""
import array
import sounddevice as sd

for device_id in (17, 16, 18, 22):
    try:
        info = sd.query_devices(device_id)
        rate = int(info['default_samplerate'])
        stream = sd.RawInputStream(device=device_id, samplerate=rate, channels=1, dtype='int16')
        stream.start()
        data, overflow = stream.read(rate * 2)
        stream.stop(); stream.close()
        samples = array.array('h'); samples.frombytes(data)
        peak = max(abs(value) for value in samples) / 32768
        print(f'{device_id}: {info["name"]} | peak={peak:.4f} | overflow={overflow}', flush=True)
    except Exception as error:
        print(f'{device_id}: unavailable ({type(error).__name__}: {error})', flush=True)
