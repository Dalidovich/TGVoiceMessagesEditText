namespace TGVoiceMessagesEditText.Services.Speech;

public interface ISpeechTranscriber
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task<TranscriptionResult> TranscribeAsync(float[] samples, CancellationToken cancellationToken);
}
