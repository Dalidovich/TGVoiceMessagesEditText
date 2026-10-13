using TGVoiceMessagesEditText.Configuration;
using TGVoiceMessagesEditText.Services.Speech;

namespace TGVoiceMessagesEditText.Tests.Speech;

public class GpuDeviceLogTests
{
    private const string VulkanLog =
        "ggml_vulkan: Found 2 Vulkan devices:\n" +
        "ggml_vulkan: 0 = NVIDIA GeForce GTX 750 (NVIDIA) | uma: 0 | fp16: 0 | warp size: 32\n" +
        "ggml_vulkan: 1 = NVIDIA GeForce GTX 1660 SUPER (NVIDIA) | uma: 0 | fp16: 1 | warp size: 32\n" +
        "whisper_model_load:      Vulkan0 total size =  1623.92 MB\n";

    private const string CudaLog =
        "ggml_cuda_init: found 2 CUDA devices (Total VRAM: 8191 MiB):\n" +
        "  Device 0: NVIDIA GeForce GTX 1660 SUPER, compute capability 7.5, VMM: yes, VRAM: 6143 MiB\n" +
        "  Device 1: NVIDIA GeForce GTX 750, compute capability 5.0, VMM: yes, VRAM: 2047 MiB\n" +
        "  Device 0: NVIDIA GeForce GTX 1660 SUPER\n" +
        "whisper_model_load:        CUDA1 total size =  1623.92 MB\n";

    [Fact]
    public void ParseDevices_ReturnsVulkanDevices()
    {
        Assert.Equal(
            [new GpuDevice(0, "NVIDIA GeForce GTX 750 (NVIDIA)"), new GpuDevice(1, "NVIDIA GeForce GTX 1660 SUPER (NVIDIA)")],
            GpuDeviceLog.ParseDevices(VulkanLog, SpeechBackend.Vulkan));
    }

    [Fact]
    public void ParseDevices_ReturnsCudaDevicesWithoutDuplicates()
    {
        Assert.Equal(
            [new GpuDevice(0, "NVIDIA GeForce GTX 1660 SUPER"), new GpuDevice(1, "NVIDIA GeForce GTX 750")],
            GpuDeviceLog.ParseDevices(CudaLog, SpeechBackend.Cuda));
    }

    [Fact]
    public void ParseDevices_ReturnsEmptyListForLogWithoutDevices()
    {
        Assert.Empty(GpuDeviceLog.ParseDevices("whisper_model_load:          CPU total size =    77.11 MB\n", SpeechBackend.Vulkan));
    }

    [Fact]
    public void ParseSelectedIndex_ReadsVulkanDeviceFromModelLoadLine()
    {
        Assert.Equal(0, GpuDeviceLog.ParseSelectedIndex(VulkanLog, SpeechBackend.Vulkan));
    }

    [Fact]
    public void ParseSelectedIndex_ReadsCudaDeviceFromModelLoadLine()
    {
        Assert.Equal(1, GpuDeviceLog.ParseSelectedIndex(CudaLog, SpeechBackend.Cuda));
    }

    [Fact]
    public void ParseSelectedIndex_ReadsBackendInitLine()
    {
        Assert.Equal(2, GpuDeviceLog.ParseSelectedIndex("whisper_backend_init_gpu: using Vulkan2 backend\n", SpeechBackend.Vulkan));
    }

    [Fact]
    public void ParseSelectedIndex_ReturnsLatestLoadAcrossReloads()
    {
        const string log =
            "whisper_model_load:      Vulkan0 total size =  1623.92 MB\n" +
            "whisper_model_load:      Vulkan1 total size =  1623.92 MB\n";

        Assert.Equal(1, GpuDeviceLog.ParseSelectedIndex(log, SpeechBackend.Vulkan));
    }

    [Fact]
    public void ParseSelectedIndex_IgnoresOtherBackend()
    {
        Assert.Null(GpuDeviceLog.ParseSelectedIndex(CudaLog, SpeechBackend.Vulkan));
    }

    [Fact]
    public void ParseSelectedIndex_ReturnsNullForCpuLoad()
    {
        Assert.Null(GpuDeviceLog.ParseSelectedIndex("whisper_model_load:          CPU total size =    77.11 MB\n", SpeechBackend.Cuda));
    }
}
