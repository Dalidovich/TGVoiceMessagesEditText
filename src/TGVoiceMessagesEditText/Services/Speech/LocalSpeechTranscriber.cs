namespace TGVoiceMessagesEditText.Services.Speech;

public sealed class LocalSpeechTranscriber(WhisperEngine whisper) : ISpeechTranscriber
{
    public Task InitializeAsync(CancellationToken cancellationToken) => whisper.InitializeAsync(cancellationToken);

    public async Task<TranscriptionResult> TranscribeAsync(float[] samples, CancellationToken cancellationToken)
    {
        var text = await whisper.TranscribeAsync(samples, cancellationToken);
        return new TranscriptionResult(text, null, samples.Length / (double)OpusVoiceDecoder.SampleRate, null);
    }
}
