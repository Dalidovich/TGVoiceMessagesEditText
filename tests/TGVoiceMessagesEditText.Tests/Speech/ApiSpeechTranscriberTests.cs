using System.Net;
using System.Text;
using TGVoiceMessagesEditText.Configuration;
using TGVoiceMessagesEditText.Services.Speech;

namespace TGVoiceMessagesEditText.Tests.Speech;

public class ApiSpeechTranscriberTests
{
    [Fact]
    public async Task TranscribeAsync_ChecksReadinessAndPostsConfiguredWavRequest()
    {
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        var settings = new AppSettings
        {
            Speech = new SpeechSettings
            {
                Api = new ApiSpeechSettings
                {
                    Endpoint = "https://example.test/transcribe",
                    RequestMethod = "POST",
                    HealthEndpoint = "https://example.test/ready",
                    HealthMethod = "GET",
                    HealthResponseProperty = "state",
                    HealthResponseValue = "available",
                    AudioContentType = "audio/wav",
                    LanguageParameter = "locale",
                    Language = "auto",
                    TextResponseProperty = "transcript",
                    LanguageResponseProperty = "detectedLocale",
                    DurationResponseProperty = "seconds",
                    BackendResponseProperty = "engine",
                    Headers = new Dictionary<string, string> { ["X-Access-Key"] = "key" }
                }
            }
        };
        var transcriber = new ApiSpeechTranscriber(client, settings);

        await transcriber.InitializeAsync(CancellationToken.None);
        var result = await transcriber.TranscribeAsync([0f, 0.5f], CancellationToken.None);

        Assert.Equal("Recognized text", result.Text);
        Assert.Equal("en", result.Language);
        Assert.Equal(2.5, result.DurationSeconds);
        Assert.Equal("test engine", result.Backend);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("https://example.test/transcribe?locale=auto", handler.Requests[1].Uri);
        Assert.Equal("audio/wav", handler.Requests[1].ContentType);
        Assert.Equal("key", handler.Requests[1].AccessKey);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(handler.Requests[1].Body, 0, 4));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<Request> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new Request(
                request.RequestUri!.ToString(),
                request.Content?.Headers.ContentType?.MediaType,
                request.Headers.GetValues("X-Access-Key").Single(),
                request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken)));

            var content = Requests.Count == 1
                ? """{"state":"available"}"""
                : """{"transcript":"Recognized text","detectedLocale":"en","seconds":2.5,"engine":"test engine"}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
        }
    }

    private sealed record Request(string Uri, string? ContentType, string AccessKey, byte[] Body);
}
