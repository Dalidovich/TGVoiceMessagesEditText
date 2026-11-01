namespace TGVoiceMessagesEditText.Configuration;

public class SpeechSettings
{
    public SpeechBackend Backend { get; set; } = SpeechBackend.Vulkan;

    public string GpuName { get; set; } = "GTX 1660";

    public int CpuThreads { get; set; }

    public string Language { get; set; } = "ru";

    public string ModelPath { get; set; } = string.Empty;

    public ApiSpeechSettings Api { get; set; } = new();

    public int ResolveCpuThreads() => CpuThreads > 0 ? CpuThreads : Environment.ProcessorCount;
}
