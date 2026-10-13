using Microsoft.Extensions.Logging;

namespace TGVoiceMessagesEditText.Services.Speech;

public static class DownloadProgress
{
    private const long StepBytes = 200L * 1024 * 1024;

    public static async Task CopyAsync(Stream source, Stream target, ILogger logger, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 1024];
        long total = 0;
        var nextReport = StepBytes;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            total += read;
            if (total >= nextReport)
            {
                logger.LogInformation("Downloaded {Megabytes} MB", total / (1024 * 1024));
                nextReport += StepBytes;
            }
        }
    }
}
