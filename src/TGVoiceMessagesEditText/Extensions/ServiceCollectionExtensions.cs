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
        services.AddSingleton<OpusVoiceDecoder>();
        services.AddHostedService<VoiceCaptionWorker>();
        return services;
    }
}
