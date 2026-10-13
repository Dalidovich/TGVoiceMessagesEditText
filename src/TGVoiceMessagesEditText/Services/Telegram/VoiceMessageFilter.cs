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
        if (!isOwn || message.fwd_from is not null || !string.IsNullOrEmpty(message.message))
        {
            return false;
        }

        return utcNow - message.date <= maxAge && GetVoiceDocument(message) is not null;
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
