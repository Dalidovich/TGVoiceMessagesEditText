using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using TGVoiceMessagesEditText.Configuration;

namespace TGVoiceMessagesEditText.Services.Speech;

public sealed class ApiSpeechTranscriber(HttpClient client, AppSettings settings) : ISpeechTranscriber
{
    private readonly ApiSpeechSettings _api = settings.Speech.Api;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        if (string.IsNullOrWhiteSpace(_api.HealthEndpoint))
        {
            return;
        }

        using var request = CreateRequest(_api.HealthMethod, _api.HealthEndpoint);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (string.IsNullOrWhiteSpace(_api.HealthResponseProperty))
        {
            return;
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty(_api.HealthResponseProperty, out var value) ||
            !string.Equals(value.ToString(), _api.HealthResponseValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The transcription API readiness response does not match the configured value.");
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(float[] samples, CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint();
        using var request = CreateRequest(_api.RequestMethod, endpoint);
        request.Content = new ByteArrayContent(WavWriter.Encode(samples, OpusVoiceDecoder.SampleRate));
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(_api.AudioContentType);

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or (HttpStatusCode)413)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new SpeechInputException(response.StatusCode, detail);
        }

        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = document.RootElement;
        if (!root.TryGetProperty(_api.TextResponseProperty, out var text))
        {
            throw new InvalidOperationException("The transcription API response does not contain the configured text property.");
        }

        var language = ReadString(root, _api.LanguageResponseProperty);
        var duration = !string.IsNullOrWhiteSpace(_api.DurationResponseProperty) &&
                       root.TryGetProperty(_api.DurationResponseProperty, out var durationValue) &&
                       durationValue.TryGetDouble(out var value)
            ? value
            : samples.Length / (double)OpusVoiceDecoder.SampleRate;
        var backend = ReadString(root, _api.BackendResponseProperty);
        return new TranscriptionResult(text.GetString() ?? string.Empty, language, duration, backend);
    }

    private HttpRequestMessage CreateRequest(string method, string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Parse(method), endpoint);
        foreach (var (name, value) in _api.Headers)
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }

        return request;
    }

    private string BuildEndpoint()
    {
        if (string.IsNullOrWhiteSpace(_api.LanguageParameter))
        {
            return _api.Endpoint;
        }

        var separator = _api.Endpoint.Contains('?') ? '&' : '?';
        return $"{_api.Endpoint}{separator}{Uri.EscapeDataString(_api.LanguageParameter)}={Uri.EscapeDataString(_api.Language)}";
    }

    private void ValidateConfiguration()
    {
        if (!Uri.TryCreate(_api.Endpoint, UriKind.Absolute, out _) ||
            string.IsNullOrWhiteSpace(_api.RequestMethod) ||
            string.IsNullOrWhiteSpace(_api.AudioContentType) ||
            string.IsNullOrWhiteSpace(_api.TextResponseProperty) ||
            _api.TimeoutSeconds < 0 ||
            (!string.IsNullOrWhiteSpace(_api.HealthEndpoint) && string.IsNullOrWhiteSpace(_api.HealthMethod)))
        {
            throw new InvalidOperationException("The API speech backend configuration is incomplete or invalid.");
        }
    }

    private static string? ReadString(JsonElement root, string propertyName) =>
        string.IsNullOrWhiteSpace(propertyName) || !root.TryGetProperty(propertyName, out var value) ? null : value.GetString();
}
