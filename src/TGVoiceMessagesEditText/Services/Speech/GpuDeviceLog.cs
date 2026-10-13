using System.Globalization;
using System.Text.RegularExpressions;
using TGVoiceMessagesEditText.Configuration;

namespace TGVoiceMessagesEditText.Services.Speech;

public static partial class GpuDeviceLog
{
    public static IReadOnlyList<GpuDevice> ParseDevices(string log, SpeechBackend backend)
    {
        var deviceLine = backend == SpeechBackend.Cuda ? CudaDeviceLine() : VulkanDeviceLine();
        return deviceLine.Matches(log)
            .Select(match => new GpuDevice(ParseIndex(match), match.Groups["name"].Value.Trim()))
            .DistinctBy(device => device.Index)
            .ToList();
    }

    public static int? ParseSelectedIndex(string log, SpeechBackend backend)
    {
        var api = backend == SpeechBackend.Cuda ? "CUDA" : "Vulkan";
        var match = SelectedLine().Matches(log).LastOrDefault(candidate => candidate.Groups["api"].Value == api);
        return match is null ? null : ParseIndex(match);
    }

    private static int ParseIndex(Match match) => int.Parse(match.Groups["index"].Value, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"ggml_vulkan:\s*(?<index>\d+)\s*=\s*(?<name>[^|\r\n]+)\|")]
    private static partial Regex VulkanDeviceLine();

    [GeneratedRegex(@"Device\s+(?<index>\d+):\s*(?<name>[^,\r\n]+)")]
    private static partial Regex CudaDeviceLine();

    [GeneratedRegex(@"whisper_model_load:\s*(?<api>Vulkan|CUDA)(?<index>\d+)\s+total size|whisper_backend_init_gpu:\s*using\s+(?<api>Vulkan|CUDA)(?<index>\d+)\s+backend")]
    private static partial Regex SelectedLine();
}
