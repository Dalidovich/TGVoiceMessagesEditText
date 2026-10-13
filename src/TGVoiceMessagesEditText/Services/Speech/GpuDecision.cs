namespace TGVoiceMessagesEditText.Services.Speech;

public record GpuDecision(GpuDecisionKind Kind, int DeviceIndex, string Reason)
{
    public static GpuDecision UseLoaded(GpuDevice device) => new(GpuDecisionKind.UseLoaded, device.Index, $"device {device.Index} ({device.Name}) is selected");

    public static GpuDecision Reload(GpuDevice device) => new(GpuDecisionKind.Reload, device.Index, $"wanted device {device.Name} has index {device.Index}");

    public static GpuDecision FallbackToCpu(string reason) => new(GpuDecisionKind.FallbackToCpu, -1, reason);
}
