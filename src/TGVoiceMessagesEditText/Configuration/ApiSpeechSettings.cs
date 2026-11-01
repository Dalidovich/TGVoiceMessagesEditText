namespace TGVoiceMessagesEditText.Configuration;

public class ApiSpeechSettings
{
    public string Endpoint { get; set; } = string.Empty;

    public string RequestMethod { get; set; } = string.Empty;

    public string HealthEndpoint { get; set; } = string.Empty;

    public string HealthMethod { get; set; } = string.Empty;

    public string HealthResponseProperty { get; set; } = string.Empty;

    public string HealthResponseValue { get; set; } = string.Empty;

    public string AudioContentType { get; set; } = string.Empty;

    public string LanguageParameter { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string TextResponseProperty { get; set; } = string.Empty;

    public string LanguageResponseProperty { get; set; } = string.Empty;

    public string DurationResponseProperty { get; set; } = string.Empty;

    public string BackendResponseProperty { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; }

    public Dictionary<string, string> Headers { get; set; } = [];
}
