namespace TGVoiceMessagesEditText.Configuration;

public class FirstRunSetup(AppSettingsStore store)
{
    public bool EnsureTelegramCredentials(AppSettings settings)
    {
        if (settings.Telegram.HasCredentials)
        {
            return true;
        }

        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Telegram api_id and api_hash are missing in settings.json.");
            return false;
        }

        Console.WriteLine("Telegram API credentials are required. Create them at https://my.telegram.org -> API development tools.");
        Console.Write("api_id: ");
        if (!int.TryParse(Console.ReadLine()?.Trim(), out var apiId) || apiId <= 0)
        {
            Console.Error.WriteLine("api_id must be a positive number.");
            return false;
        }

        Console.Write("api_hash: ");
        var apiHash = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(apiHash))
        {
            Console.Error.WriteLine("api_hash must not be empty.");
            return false;
        }

        settings.Telegram.ApiId = apiId;
        settings.Telegram.ApiHash = apiHash;
        store.Save(settings);
        return true;
    }
}
