namespace TGVoiceMessagesEditText.Services.Speech;

public sealed record TranscriptionResult(string Text, string? Language, double DurationSeconds, string? Backend);
