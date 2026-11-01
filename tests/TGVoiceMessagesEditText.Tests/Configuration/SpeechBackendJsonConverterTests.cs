using System.Text.Json;
using TGVoiceMessagesEditText.Configuration;

namespace TGVoiceMessagesEditText.Tests.Configuration;

public class SpeechBackendJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new SpeechBackendJsonConverter() }
    };

    [Theory]
    [InlineData("\"Cpu\"", SpeechBackend.Cpu)]
    [InlineData("\"cpu\"", SpeechBackend.Cpu)]
    [InlineData("\"Vulkan\"", SpeechBackend.Vulkan)]
    [InlineData("\"CUDA\"", SpeechBackend.Cuda)]
    [InlineData("\"Api\"", SpeechBackend.Api)]
    [InlineData("\"OpenVino\"", SpeechBackend.Vulkan)]
    [InlineData("7", SpeechBackend.Vulkan)]
    public void Read_ParsesKnownValuesAndFallsBackOnUnknown(string json, SpeechBackend expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<SpeechBackend>(json, Options));
    }

    [Fact]
    public void Read_KeepsOtherSettingsWhenBackendIsUnknown()
    {
        const string json = """{"Telegram":{"ApiId":12345,"ApiHash":"hash"},"Speech":{"Backend":"OpenVino","GpuName":"GTX 1660"}}""";

        var settings = JsonSerializer.Deserialize<AppSettings>(json, Options)!;

        Assert.Equal(12345, settings.Telegram.ApiId);
        Assert.Equal("hash", settings.Telegram.ApiHash);
        Assert.Equal(SpeechBackend.Vulkan, settings.Speech.Backend);
    }
}
