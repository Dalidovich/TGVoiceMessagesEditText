using System.Text;

namespace TGVoiceMessagesEditText.Services.Speech;

public static class WavWriter
{
    private const short BitsPerSample = 16;
    private const short ChannelCount = 1;
    private const int HeaderSize = 44;

    public static void Write(string path, float[] samples, int sampleRate)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        Write(stream, samples, sampleRate);
    }

    public static byte[] Encode(float[] samples, int sampleRate)
    {
        using var stream = new MemoryStream();
        Write(stream, samples, sampleRate);
        return stream.ToArray();
    }

    private static void Write(Stream stream, float[] samples, int sampleRate)
    {
        var dataSize = samples.Length * sizeof(short);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, true);

        writer.Write("RIFF"u8);
        writer.Write(HeaderSize - 8 + dataSize);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(ChannelCount);
        writer.Write(sampleRate);
        writer.Write(sampleRate * ChannelCount * BitsPerSample / 8);
        writer.Write((short)(ChannelCount * BitsPerSample / 8));
        writer.Write(BitsPerSample);
        writer.Write("data"u8);
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write((short)Math.Clamp(sample * 32768f, short.MinValue, short.MaxValue));
        }
    }
}
