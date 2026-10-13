using System.Text.Json;

namespace TGVoiceMessagesEditText.Configuration;

public class AppSettingsStore(PortablePaths paths)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new SpeechBackendJsonConverter() }
    };

    public AppSettings Load()
    {
        var settings = Read();
        Save(settings);
        return settings;
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            File.WriteAllText(paths.SettingsFile, JsonSerializer.Serialize(settings, SerializerOptions));
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Settings were not saved to {paths.SettingsFile}: {exception.Message}");
            return false;
        }
    }

    private AppSettings Read()
    {
        try
        {
            if (!File.Exists(paths.SettingsFile))
            {
                return new AppSettings();
            }

            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(paths.SettingsFile), SerializerOptions) ?? new AppSettings();
            settings.Telegram ??= new TelegramSettings();
            settings.Speech ??= new SpeechSettings();
            return settings;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Settings file {paths.SettingsFile} is unreadable, defaults are used: {exception.Message}");
            return new AppSettings();
        }
    }
}
