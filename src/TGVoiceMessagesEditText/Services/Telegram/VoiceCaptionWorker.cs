using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TGVoiceMessagesEditText.Configuration;
using TGVoiceMessagesEditText.Services.Speech;
using TL;
using WTelegram;

namespace TGVoiceMessagesEditText.Services.Telegram;

public class VoiceCaptionWorker(
    AppSettings settings,
    PortablePaths paths,
    WhisperEngine whisper,
    OpusVoiceDecoder decoder,
    IHostApplicationLifetime lifetime,
    ILogger<VoiceCaptionWorker> logger) : BackgroundService
{
    private const int DefaultCaptionLimit = 1024;
    private const int PremiumCaptionLimit = 2048;

    private readonly Channel<Message> _queue = System.Threading.Channels.Channel.CreateUnbounded<Message>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Voice caption worker failed");
            Environment.ExitCode = 1;
            lifetime.StopApplication();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await whisper.InitializeAsync(cancellationToken);

        var writable = paths.TryEnsureWritableDirectory(paths.DataDirectory);
        if (!writable)
        {
            logger.LogWarning("Application folder is read-only, the Telegram session lives in memory for this run only");
        }

        Helpers.Log = (level, message) => logger.Log((LogLevel)level, "{TelegramLog}", message);
        using var sessionStore = writable ? null : new MemoryStream();
        using var client = new Client(ProvideConfig, sessionStore);
        var manager = client.WithUpdateManager(OnUpdate, writable ? paths.UpdatesStateFile : null);
        var self = await client.LoginUserIfNeeded();
        var captionLimit = self.flags.HasFlag(User.Flags.premium) ? PremiumCaptionLimit : DefaultCaptionLimit;
        logger.LogInformation("Logged in as {UserId}, caption limit is {CaptionLimit} characters", self.id, captionLimit);

        try
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(cancellationToken))
            {
                await CaptionSafelyAsync(client, manager, self, message, captionLimit, cancellationToken);
            }
        }
        finally
        {
            if (writable)
            {
                manager.SaveState(paths.UpdatesStateFile);
            }
        }
    }

    private Task OnUpdate(Update update)
    {
        if (update is UpdateNewMessage { message: Message message } && VoiceMessageFilter.GetVoiceDocument(message) is not null)
        {
            _queue.Writer.TryWrite(message);
        }

        return Task.CompletedTask;
    }

    private async Task CaptionSafelyAsync(Client client, UpdateManager manager, User self, Message message, int captionLimit, CancellationToken cancellationToken)
    {
        try
        {
            await CaptionAsync(client, manager, self, message, captionLimit, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to caption voice message {MessageId}", message.id);
        }
    }

    private async Task CaptionAsync(Client client, UpdateManager manager, User self, Message message, int captionLimit, CancellationToken cancellationToken)
    {
        var maxAge = TimeSpan.FromHours(settings.Telegram.MaxMessageAgeHours);
        if (!VoiceMessageFilter.IsCaptionCandidate(message, self.id, DateTime.UtcNow, maxAge))
        {
            return;
        }

        var peer = ResolvePeer(manager, self, message.peer_id.ID);
        if (peer is null)
        {
            logger.LogDebug("Voice message {MessageId} is skipped: the chat is a bot or is unknown", message.id);
            return;
        }

        if (!VoiceMessageFilter.HasProcessingPlaceholder(message))
        {
            await client.Messages_EditMessage(peer, message.id, message: CaptionFormatter.ProcessingPlaceholder);
        }

        string text;
        double seconds;
        try
        {
            (text, seconds) = await RecognizeAsync(client, message, cancellationToken);
        }
        catch
        {
            await ClearPlaceholderSafelyAsync(client, peer, message);
            throw;
        }

        if (text.Length == 0)
        {
            logger.LogInformation("Voice message {MessageId} produced no text", message.id);
            await ClearPlaceholderSafelyAsync(client, peer, message);
            return;
        }

        var caption = CaptionFormatter.Format(text, captionLimit);
        await client.Messages_EditMessage(peer, message.id, message: caption);
        logger.LogInformation(
            "Captioned voice message {MessageId}: {Seconds:F1} s of audio, {Length} characters",
            message.id,
            seconds,
            caption.Length);
    }

    private async Task<(string Text, double Seconds)> RecognizeAsync(Client client, Message message, CancellationToken cancellationToken)
    {
        var document = VoiceMessageFilter.GetVoiceDocument(message)!;
        using var audio = new MemoryStream();
        await client.DownloadFileAsync(document, audio);
        logger.LogInformation(
            "Downloaded voice message {MessageId}: {Downloaded} bytes, document size {DocumentSize} bytes, mime {MimeType}",
            message.id,
            audio.Length,
            document.size,
            document.mime_type);
        audio.Position = 0;

        var samples = decoder.Decode(audio);
        logger.LogInformation(
            "Decoded voice message {MessageId}: {SampleCount} samples, peak {Peak:F3}",
            message.id,
            samples.Length,
            samples.Length == 0 ? 0f : samples.Max(Math.Abs));
        if (settings.TestMode)
        {
            SaveTestRecording(message, audio.ToArray(), samples);
        }

        var text = await whisper.TranscribeAsync(samples, cancellationToken);
        return (text, samples.Length / (double)OpusVoiceDecoder.SampleRate);
    }

    private async Task ClearPlaceholderSafelyAsync(Client client, InputPeer peer, Message message)
    {
        try
        {
            await client.Messages_EditMessage(peer, message.id, message: string.Empty);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to remove the processing placeholder from voice message {MessageId}", message.id);
        }
    }

    private void SaveTestRecording(Message message, byte[] original, float[] samples)
    {
        try
        {
            if (!paths.TryEnsureWritableDirectory(paths.TestRecordingsDirectory))
            {
                logger.LogWarning("Test recordings folder is not writable: {Directory}", paths.TestRecordingsDirectory);
                return;
            }

            var baseName = Path.Combine(paths.TestRecordingsDirectory, $"{DateTime.Now:yyyyMMdd-HHmmss}-{message.peer_id.ID}-{message.id}");
            File.WriteAllBytes(baseName + ".ogg", original);
            WavWriter.Write(baseName + ".wav", samples, OpusVoiceDecoder.SampleRate);
            logger.LogInformation("Test mode: saved recording to {FilePath}", baseName + ".wav");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Test mode: failed to save recording of message {MessageId}", message.id);
        }
    }

    private static InputPeer? ResolvePeer(UpdateManager manager, User self, long userId)
    {
        if (userId == self.id)
        {
            return InputPeer.Self;
        }

        if (!manager.Users.TryGetValue(userId, out var user) || user.flags.HasFlag(User.Flags.bot))
        {
            return null;
        }

        return user.ToInputPeer();
    }

    private string? ProvideConfig(string what)
    {
        return what switch
        {
            "api_id" => settings.Telegram.ApiId.ToString(),
            "api_hash" => settings.Telegram.ApiHash,
            "session_pathname" => paths.SessionFile,
            "phone_number" => string.IsNullOrWhiteSpace(settings.Telegram.PhoneNumber)
                ? Prompt("Phone number (international format): ")
                : settings.Telegram.PhoneNumber.Trim(),
            "verification_code" => Prompt("Verification code: "),
            "password" => Prompt("Two-step verification password: "),
            _ => null
        };
    }

    private static string? Prompt(string label)
    {
        Console.Write(label);
        return Console.ReadLine()?.Trim();
    }
}
