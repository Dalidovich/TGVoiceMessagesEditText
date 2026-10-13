namespace TGVoiceMessagesEditText.Configuration;

public class TelegramSettings
{
    public int ApiId { get; set; }

    public string ApiHash { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public int MaxMessageAgeHours { get; set; } = 24;

    public bool HasCredentials => ApiId > 0 && !string.IsNullOrWhiteSpace(ApiHash);
}
