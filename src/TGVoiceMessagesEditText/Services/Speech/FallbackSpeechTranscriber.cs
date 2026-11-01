using Microsoft.Extensions.Logging;

namespace TGVoiceMessagesEditText.Services.Speech;

public sealed class FallbackSpeechTranscriber(
    ApiSpeechTranscriber api,
    LocalSpeechTranscriber local,
    ILogger<FallbackSpeechTranscriber> logger) : ISpeechTranscriber
{
    private readonly SemaphoreSlim _fallbackLock = new(1, 1);
    private ISpeechTranscriber _active = api;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await api.InitializeAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ActivateLocalAsync(exception, cancellationToken);
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(float[] samples, CancellationToken cancellationToken)
    {
        try
        {
            return await _active.TranscribeAsync(samples, cancellationToken);
        }
        catch (HttpRequestException exception) when (ReferenceEquals(_active, api))
        {
            await ActivateLocalAsync(exception, cancellationToken);
            return await local.TranscribeAsync(samples, cancellationToken);
        }
    }

    private async Task ActivateLocalAsync(Exception exception, CancellationToken cancellationToken)
    {
        await _fallbackLock.WaitAsync(cancellationToken);
        try
        {
            if (ReferenceEquals(_active, local))
            {
                return;
            }

            logger.LogWarning(exception, "Transcription API is unavailable, switching to the local speech backend");
            await local.InitializeAsync(cancellationToken);
            _active = local;
        }
        finally
        {
            _fallbackLock.Release();
        }
    }
}
