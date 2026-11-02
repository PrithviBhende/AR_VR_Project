using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace FerrariLab
{
    // Bounded PCM16 buffers; no recording is written to disk by the Unity client.
    public static class ZenoAudio
    {
        public static float[] ResampleMono(float[] samples, int sourceRate, int targetRate = 16000)
        {
            if (sourceRate <= 0 || targetRate <= 0) throw new ArgumentOutOfRangeException();
            if (sourceRate == targetRate) return samples;
            var output = new float[(int)((long)samples.Length * targetRate / sourceRate)];
            double ratio = (double)sourceRate / targetRate;
            for (int i = 0; i < output.Length; i++)
            {
                // Average source coverage instead of simply discarding samples.
                double start = i * ratio, end = (i + 1) * ratio, sum = 0;
                for (int j = (int)start; j < Math.Ceiling(end) && j < samples.Length; j++)
                    sum += samples[j] * (Math.Min(end, j + 1) - Math.Max(start, j));
                output[i] = (float)(sum / ratio);
            }
            return output;
        }

        public static byte[] Encode(float[] samples, int sampleRate = 16000)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples.Length * 2);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(sampleRate);
                writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
                foreach (var sample in samples) writer.Write((short)(Mathf.Clamp(sample, -1, 1) * 32767));
                return stream.ToArray();
            }
        }

        public static AudioClip Decode(string encoded)
        {
            var bytes = Convert.FromBase64String(encoded);
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") throw new FormatException("Not WAV");
                reader.ReadInt32();
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") throw new FormatException("Not WAV");
                int channels = 0, rate = 0, bits = 0, format = 0;
                byte[] pcm = null;
                while (stream.Position + 8 <= stream.Length)
                {
                    string id = Encoding.ASCII.GetString(reader.ReadBytes(4)); int size = reader.ReadInt32();
                    long next = stream.Position + size + (size & 1);
                    if (size < 0 || next > stream.Length) throw new FormatException("Invalid WAV chunk");
                    if (id == "fmt " && size >= 16)
                    {
                        format = reader.ReadUInt16(); channels = reader.ReadUInt16(); rate = reader.ReadInt32();
                        reader.ReadInt32(); reader.ReadUInt16(); bits = reader.ReadUInt16();
                    }
                    if (id == "data") pcm = reader.ReadBytes(size);
                    stream.Position = next;
                }
                if (format != 1 || bits != 16 || channels < 1 || channels > 2 || rate < 8000 || rate > 96000 || pcm == null || pcm.Length == 0 || pcm.Length % (2 * channels) != 0)
                    throw new FormatException("Unsupported WAV format");
                var samples = new float[pcm.Length / 2];
                for (int i = 0; i < samples.Length; i++) samples[i] = BitConverter.ToInt16(pcm, i * 2) / 32768f;
                var clip = AudioClip.Create("Zeno reply", samples.Length / channels, channels, rate, false);
                clip.SetData(samples, 0); return clip;
            }
        }
    }
}
