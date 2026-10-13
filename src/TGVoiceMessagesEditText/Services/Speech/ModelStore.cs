using Microsoft.Extensions.Logging;
using TGVoiceMessagesEditText.Configuration;
using Whisper.net.Ggml;

namespace TGVoiceMessagesEditText.Services.Speech;

public class ModelStore(AppSettings settings, PortablePaths paths, ILogger<ModelStore> logger)
{
    public async Task<string> EnsureModelAsync(CancellationToken cancellationToken)
    {
        var modelPath = string.IsNullOrWhiteSpace(settings.Speech.ModelPath)
            ? paths.DefaultModelFile
            : Path.GetFullPath(settings.Speech.ModelPath, paths.Root);

        if (File.Exists(modelPath))
        {
            return modelPath;
        }

        var directory = Path.GetDirectoryName(modelPath)!;
        if (!paths.TryEnsureWritableDirectory(directory))
        {
            throw new InvalidOperationException($"Model file {modelPath} is missing and its folder is read-only.");
        }

        logger.LogInformation("Downloading ggml-large-v3-turbo model to {ModelPath}", modelPath);
        var partialPath = modelPath + ".partial";
        await using (var source = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.LargeV3Turbo, cancellationToken: cancellationToken))
        await using (var target = new FileStream(partialPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await DownloadProgress.CopyAsync(source, target, logger, cancellationToken);
        }

        File.Move(partialPath, modelPath, true);
        logger.LogInformation("Model download completed");
        return modelPath;
    }
}
