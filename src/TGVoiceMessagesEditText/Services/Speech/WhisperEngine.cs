using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TGVoiceMessagesEditText.Configuration;
using Whisper.net;
using Whisper.net.LibraryLoader;
using Whisper.net.Logger;

namespace TGVoiceMessagesEditText.Services.Speech;

public sealed partial class WhisperEngine(
    AppSettings settings,
    ModelStore modelStore,
    CudaRuntimeStore cudaRuntime,
    ILogger<WhisperEngine> logger) : IDisposable
{
    private const int MaxGpuLoadAttempts = 2;

    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;
    private bool _usesGpu;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var modelPath = await modelStore.EnsureModelAsync(cancellationToken);
        var backend = settings.Speech.Backend;
        var cpuReason = "CPU backend is configured";
        if (backend == SpeechBackend.Api)
        {
            backend = SpeechBackend.Vulkan;
            cpuReason = "the transcription API is unavailable";
        }

        if (backend == SpeechBackend.Cuda && await TryLoadCudaRuntimeAsync(cancellationToken) is { } failure)
        {
            backend = SpeechBackend.Cpu;
            cpuReason = failure;
        }

        _factory = backend == SpeechBackend.Cpu
            ? CreateCpuFactory(modelPath, cpuReason)
            : CreateGpuFactory(modelPath, backend, out cpuReason) ?? CreateCpuFactory(modelPath, cpuReason);

        var builder = _factory.CreateBuilder().WithLanguage(settings.Speech.Language);
        if (!_usesGpu)
        {
            builder = builder.WithThreads(settings.Speech.ResolveCpuThreads());
        }

        _processor = builder.Build();
    }

    public async Task<string> TranscribeAsync(float[] samples, CancellationToken cancellationToken)
    {
        if (_processor is null)
        {
            throw new InvalidOperationException("Whisper engine is not initialized.");
        }

        var text = new StringBuilder();
        await foreach (var segment in _processor.ProcessAsync(samples, cancellationToken))
        {
            text.Append(segment.Text);
        }

        return Whitespace().Replace(text.ToString(), " ").Trim();
    }

    public void Dispose()
    {
        _processor?.Dispose();
        _factory?.Dispose();
    }

    private async Task<string?> TryLoadCudaRuntimeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await cudaRuntime.EnsureLoadedAsync(cancellationToken);
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "CUDA runtime libraries could not be prepared");
            return "CUDA runtime libraries are unavailable";
        }
    }

    private WhisperFactory? CreateGpuFactory(string modelPath, SpeechBackend backend, out string fallbackReason)
    {
        var gpuLibrary = backend == SpeechBackend.Cuda ? RuntimeLibrary.Cuda12 : RuntimeLibrary.Vulkan;
        RuntimeOptions.RuntimeLibraryOrder = [gpuLibrary, RuntimeLibrary.Cpu];
        fallbackReason = "the wanted GPU device could not be selected";
        var gpuDevice = 0;
        var log = new StringBuilder();
        for (var attempt = 1; attempt <= MaxGpuLoadAttempts; attempt++)
        {
            var factory = LoadGpuFactory(modelPath, gpuDevice, log);
            if (RuntimeOptions.LoadedLibrary != gpuLibrary)
            {
                factory.Dispose();
                fallbackReason = $"{gpuLibrary} runtime was not loaded (loaded: {RuntimeOptions.LoadedLibrary})";
                return null;
            }

            var loadLog = log.ToString();
            var decision = GpuDeviceSelector.Decide(
                GpuDeviceLog.ParseDevices(loadLog, backend),
                GpuDeviceLog.ParseSelectedIndex(loadLog, backend),
                settings.Speech.GpuName);
            if (decision.Kind == GpuDecisionKind.UseLoaded)
            {
                logger.LogInformation("Whisper runs on {Backend}: {Reason}", backend, decision.Reason);
                _usesGpu = true;
                return factory;
            }

            factory.Dispose();
            if (decision.Kind == GpuDecisionKind.FallbackToCpu)
            {
                fallbackReason = decision.Reason;
                return null;
            }

            logger.LogInformation("Reloading Whisper model: {Reason}", decision.Reason);
            gpuDevice = decision.DeviceIndex;
        }

        return null;
    }

    private WhisperFactory CreateCpuFactory(string modelPath, string reason)
    {
        if (RuntimeOptions.LoadedLibrary is null)
        {
            RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Cpu];
        }

        logger.LogWarning("Whisper runs on CPU with {Threads} threads: {Reason}", settings.Speech.ResolveCpuThreads(), reason);
        return WhisperFactory.FromPath(modelPath, new WhisperFactoryOptions { UseGpu = false });
    }

    private static WhisperFactory LoadGpuFactory(string modelPath, int gpuDevice, StringBuilder log)
    {
        using (LogProvider.AddLogger((_, message) =>
               {
                   lock (log)
                   {
                       log.Append(message);
                   }
               }))
        {
            return WhisperFactory.FromPath(modelPath, new WhisperFactoryOptions { UseGpu = true, GpuDevice = gpuDevice });
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
