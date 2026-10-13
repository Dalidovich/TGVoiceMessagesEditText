namespace TGVoiceMessagesEditText.Services.Telegram;

public static class CaptionFormatter
{
    private const string Ellipsis = "…";

    public static string Format(string text, int maxLength)
    {
        var trimmed = text.Trim();
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        var cut = maxLength - Ellipsis.Length;
        if (char.IsHighSurrogate(trimmed[cut - 1]))
        {
            cut--;
        }

        return trimmed[..cut].TrimEnd() + Ellipsis;
    }
}
