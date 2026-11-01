using Microsoft.Extensions.DependencyInjection;
using TGVoiceMessagesEditText.Configuration;
using TGVoiceMessagesEditText.Services.Speech;
using TGVoiceMessagesEditText.Services.Telegram;

namespace TGVoiceMessagesEditText.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVoiceCaptioning(this IServiceCollection services, PortablePaths paths, AppSettings settings)
    {
        services.AddSingleton(paths);
        services.AddSingleton(settings);
        services.AddSingleton<ModelStore>();
        services.AddSingleton<CudaRuntimeStore>();
        services.AddSingleton<WhisperEngine>();
        services.AddSingleton<LocalSpeechTranscriber>();
        if (settings.Speech.Backend == SpeechBackend.Api)
        {
            services.AddSingleton(new HttpClient
            {
                Timeout = settings.Speech.Api.TimeoutSeconds <= 0
                    ? Timeout.InfiniteTimeSpan
                    : TimeSpan.FromSeconds(settings.Speech.Api.TimeoutSeconds)
            });
            services.AddSingleton<ApiSpeechTranscriber>();
            services.AddSingleton<ISpeechTranscriber, FallbackSpeechTranscriber>();
        }
        else
        {
            services.AddSingleton<ISpeechTranscriber>(serviceProvider => serviceProvider.GetRequiredService<LocalSpeechTranscriber>());
        }
        services.AddSingleton<OpusVoiceDecoder>();
        services.AddHostedService<VoiceCaptionWorker>();
        return services;
    }
}
