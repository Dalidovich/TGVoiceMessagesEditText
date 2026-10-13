namespace TGVoiceMessagesEditText.Services.Speech;

public static class GpuDeviceSelector
{
    public static GpuDecision Decide(IReadOnlyList<GpuDevice> devices, int? selectedIndex, string wantedName)
    {
        if (string.IsNullOrWhiteSpace(wantedName))
        {
            return GpuDecision.FallbackToCpu("GPU name is not configured");
        }

        var wanted = devices.FirstOrDefault(device => device.Name.Contains(wantedName, StringComparison.OrdinalIgnoreCase));
        if (wanted is null)
        {
            return GpuDecision.FallbackToCpu($"no GPU device matches '{wantedName}'");
        }

        if (selectedIndex is null)
        {
            return GpuDecision.FallbackToCpu("the selected GPU device could not be determined");
        }

        return selectedIndex == wanted.Index ? GpuDecision.UseLoaded(wanted) : GpuDecision.Reload(wanted);
    }
}
