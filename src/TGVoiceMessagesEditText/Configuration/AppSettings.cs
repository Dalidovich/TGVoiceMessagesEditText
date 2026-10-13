namespace TGVoiceMessagesEditText.Configuration;

public class AppSettings
{
    public TelegramSettings Telegram { get; set; } = new();

    public SpeechSettings Speech { get; set; } = new();

    public bool TestMode { get; set; }
}
