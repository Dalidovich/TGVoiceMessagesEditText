using TGVoiceMessagesEditText.Services.Telegram;
using TL;

namespace TGVoiceMessagesEditText.Tests.Telegram;

public class VoiceMessageFilterTests
{
    private const long SelfId = 100;
    private const long FriendId = 200;
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    [Fact]
    public void IsCaptionCandidate_AcceptsOwnVoiceInPrivateChat()
    {
        Assert.True(VoiceMessageFilter.IsCaptionCandidate(CreateVoice(), SelfId, Now, MaxAge));
    }

    [Fact]
    public void IsCaptionCandidate_AcceptsVoiceInSavedMessages()
    {
        var message = CreateVoice();
        message.flags = 0;
        message.peer_id = new PeerUser { user_id = SelfId };

        Assert.True(VoiceMessageFilter.IsCaptionCandidate(message, SelfId, Now, MaxAge));
    }

    [Fact]
    public void IsCaptionCandidate_RejectsIncomingVoice()
    {
        var message = CreateVoice();
        message.flags = 0;

        Assert.False(VoiceMessageFilter.IsCaptionCandidate(message, SelfId, Now, MaxAge));
    }

    [Fact]
    public void IsCaptionCandidate_RejectsGroupChat()
    {
        var message = CreateVoice();
        message.peer_id = new PeerChat { chat_id = 300 };

        Assert.False(VoiceMessageFilter.IsCaptionCandidate(message, SelfId, Now, MaxAge));
    }

    [Fact]
    public void IsCaptionCandidate_RejectsForwardedVoice()
    {
        var message = CreateVoice();
        message.fwd_from = new MessageFwdHeader();

        Assert.False(VoiceMessageFilter.IsCaptionCandidate(message, SelfId, Now, MaxAge));
    }

    [Fact]
    public void IsCaptionCandidate_RejectsVoiceWithExistingCaption()
    {
        var message = CreateVoice();
        message.message = "already captioned";

        Assert.False(VoiceMessageFilter.IsCaptionCandidate(message, SelfId, Now, MaxAge));
    }

    [Fact]
    public void IsCaptionCandidate_RejectsOldVoice()
    {
        var message = CreateVoice();
        message.date = Now.AddHours(-25);

        Assert.False(VoiceMessageFilter.IsCaptionCandidate(message, SelfId, Now, MaxAge));
    }

    [Fact]
    public void IsCaptionCandidate_RejectsMusicFile()
    {
        var message = CreateVoice();
        message.media = new MessageMediaDocument
        {
            document = new Document { attributes = [new DocumentAttributeAudio()] }
        };

        Assert.False(VoiceMessageFilter.IsCaptionCandidate(message, SelfId, Now, MaxAge));
    }

    private static Message CreateVoice()
    {
        return new Message
        {
            flags = Message.Flags.out_,
            peer_id = new PeerUser { user_id = FriendId },
            date = Now.AddMinutes(-1),
            message = string.Empty,
            media = new MessageMediaDocument
            {
                document = new Document
                {
                    attributes = [new DocumentAttributeAudio { flags = DocumentAttributeAudio.Flags.voice }]
                }
            }
        };
    }
}
