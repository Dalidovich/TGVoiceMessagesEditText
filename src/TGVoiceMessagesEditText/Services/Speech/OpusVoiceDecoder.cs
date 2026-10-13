using Concentus;
using Concentus.Oggfile;

namespace TGVoiceMessagesEditText.Services.Speech;

public class OpusVoiceDecoder
{
    public const int SampleRate = 16000;

    private const int DecodeSampleRate = 48000;
    private const int DecimationFactor = DecodeSampleRate / SampleRate;
    private const int FilterHalfLength = 24;
    private const double CutoffRatio = 0.45 / DecimationFactor;

    private static readonly double[] FilterTaps = BuildFilterTaps();

    public float[] Decode(Stream oggOpus)
    {
        var decoder = OpusCodecFactory.CreateDecoder(DecodeSampleRate, 1);
        var reader = new OpusOggReadStream(decoder, oggOpus);
        var wide = new List<float>();
        while (reader.HasNextPacket)
        {
            var packet = reader.DecodeNextPacket();
            if (packet is null)
            {
                continue;
            }

            foreach (var sample in packet)
            {
                wide.Add(sample / 32768f);
            }
        }

        return Downsample(wide);
    }

    private static float[] Downsample(List<float> wide)
    {
        var result = new float[wide.Count / DecimationFactor];
        for (var i = 0; i < result.Length; i++)
        {
            var center = i * DecimationFactor;
            double sum = 0;
            for (var tap = -FilterHalfLength; tap <= FilterHalfLength; tap++)
            {
                var index = center + tap;
                if (index >= 0 && index < wide.Count)
                {
                    sum += wide[index] * FilterTaps[tap + FilterHalfLength];
                }
            }

            result[i] = (float)sum;
        }

        return result;
    }

    private static double[] BuildFilterTaps()
    {
        var taps = new double[FilterHalfLength * 2 + 1];
        double total = 0;
        for (var i = 0; i < taps.Length; i++)
        {
            var offset = i - FilterHalfLength;
            var sinc = offset == 0 ? 2 * CutoffRatio : Math.Sin(2 * Math.PI * CutoffRatio * offset) / (Math.PI * offset);
            var window = 0.5 + 0.5 * Math.Cos(Math.PI * offset / (FilterHalfLength + 1));
            taps[i] = sinc * window;
            total += taps[i];
        }

        for (var i = 0; i < taps.Length; i++)
        {
            taps[i] /= total;
        }

        return taps;
    }
}
