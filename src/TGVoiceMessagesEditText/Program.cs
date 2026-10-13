using Microsoft.Extensions.Hosting;
using TGVoiceMessagesEditText.Configuration;
using TGVoiceMessagesEditText.Extensions;

namespace TGVoiceMessagesEditText;

public class Program
{
    public static async Task Main(string[] args)
    {
        var paths = new PortablePaths();
        var store = new AppSettingsStore(paths);
        var settings = store.Load();
        if (!new FirstRunSetup(store).EnsureTelegramCredentials(settings))
        {
            Environment.ExitCode = 1;
            return;
        }

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = paths.Root
        });
        builder.Services.AddVoiceCaptioning(paths, settings);

        await builder.Build().RunAsync();
    }
}
