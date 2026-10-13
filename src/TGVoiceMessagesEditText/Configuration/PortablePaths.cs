namespace TGVoiceMessagesEditText.Configuration;

public class PortablePaths
{
    public PortablePaths()
    {
        Root = Path.GetDirectoryName(Environment.ProcessPath) is { Length: > 0 } directory
            ? directory
            : Directory.GetCurrentDirectory();
    }

    public string Root { get; }

    public string SettingsFile => Path.Combine(Root, "settings.json");

    public string DataDirectory => Path.Combine(Root, "data");

    public string SessionFile => Path.Combine(DataDirectory, "telegram.session");

    public string UpdatesStateFile => Path.Combine(DataDirectory, "updates.state");

    public string TestRecordingsDirectory => Path.Combine(Root, "test-recordings");

    public string ModelsDirectory => Path.Combine(Root, "models");

    public string CudaDirectory => Path.Combine(Root, "cuda");

    public string DefaultModelFile => Path.Combine(ModelsDirectory, "ggml-large-v3-turbo.bin");

    public bool TryEnsureWritableDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, Path.GetRandomFileName());
            using var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
