using TL;

namespace TGVoiceMessagesEditText.Services.Telegram;

public static class VoiceMessageFilter
{
    public static bool IsCaptionCandidate(Message message, long selfId, DateTime utcNow, TimeSpan maxAge)
    {
        if (message.peer_id is not PeerUser peer)
        {
            return false;
        }

        var isOwn = message.flags.HasFlag(Message.Flags.out_) || peer.user_id == selfId;
        if (!isOwn || message.fwd_from is not null || HasRealCaption(message))
        {
            return false;
        }

        return utcNow - message.date <= maxAge && GetVoiceDocument(message) is not null;
    }

    public static bool HasProcessingPlaceholder(Message message)
    {
        return message.message == CaptionFormatter.ProcessingPlaceholder;
    }

    private static bool HasRealCaption(Message message)
    {
        return !string.IsNullOrEmpty(message.message) && !HasProcessingPlaceholder(message);
    }

    public static Document? GetVoiceDocument(Message message)
    {
        if (message.media is not MessageMediaDocument { document: Document document })
        {
            return null;
        }

        var isVoice = document.attributes?
            .OfType<DocumentAttributeAudio>()
            .Any(audio => audio.flags.HasFlag(DocumentAttributeAudio.Flags.voice)) ?? false;
        return isVoice ? document : null;
    }
}
