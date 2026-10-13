using TGVoiceMessagesEditText.Services.Telegram;

namespace TGVoiceMessagesEditText.Tests.Telegram;

public class CaptionFormatterTests
{
    [Fact]
    public void Format_KeepsTextThatFits()
    {
        Assert.Equal("hello world", CaptionFormatter.Format("  hello world ", 11));
    }

    [Fact]
    public void Format_TruncatesWithEllipsisWithinLimit()
    {
        var caption = CaptionFormatter.Format(new string('a', 2000), 1024);

        Assert.Equal(1024, caption.Length);
        Assert.EndsWith("…", caption);
    }

    [Fact]
    public void Format_DropsTrailingSpaceBeforeEllipsis()
    {
        Assert.Equal("abc…", CaptionFormatter.Format("abc def", 5));
    }

    [Fact]
    public void Format_DoesNotSplitSurrogatePair()
    {
        var caption = CaptionFormatter.Format("ab\U0001F600cd", 4);

        Assert.Equal("ab…", caption);
    }
}
