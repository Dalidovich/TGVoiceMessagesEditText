using TGVoiceMessagesEditText.Services.Speech;

namespace TGVoiceMessagesEditText.Tests.Speech;

public class GpuDeviceSelectorTests
{
    private static readonly GpuDevice[] Devices =
    [
        new(0, "NVIDIA GeForce GTX 750 (NVIDIA)"),
        new(1, "NVIDIA GeForce GTX 1660 SUPER (NVIDIA)"),
        new(2, "Intel(R) UHD Graphics 630 (Intel Corporation)")
    ];

    [Fact]
    public void Decide_UsesLoadedFactoryWhenWantedDeviceIsSelected()
    {
        var decision = GpuDeviceSelector.Decide(Devices, 1, "GTX 1660");

        Assert.Equal(GpuDecisionKind.UseLoaded, decision.Kind);
        Assert.Equal(1, decision.DeviceIndex);
    }

    [Fact]
    public void Decide_RequestsReloadWhenAnotherDeviceIsSelected()
    {
        var decision = GpuDeviceSelector.Decide(Devices, 0, "gtx 1660");

        Assert.Equal(GpuDecisionKind.Reload, decision.Kind);
        Assert.Equal(1, decision.DeviceIndex);
    }

    [Fact]
    public void Decide_FallsBackToCpuWhenNoDeviceMatches()
    {
        Assert.Equal(GpuDecisionKind.FallbackToCpu, GpuDeviceSelector.Decide(Devices, 0, "RTX 4090").Kind);
    }

    [Fact]
    public void Decide_FallsBackToCpuWhenSelectedDeviceIsUnknown()
    {
        Assert.Equal(GpuDecisionKind.FallbackToCpu, GpuDeviceSelector.Decide(Devices, null, "GTX 1660").Kind);
    }

    [Fact]
    public void Decide_FallsBackToCpuWhenNameIsNotConfigured()
    {
        Assert.Equal(GpuDecisionKind.FallbackToCpu, GpuDeviceSelector.Decide(Devices, 1, " ").Kind);
    }
}
