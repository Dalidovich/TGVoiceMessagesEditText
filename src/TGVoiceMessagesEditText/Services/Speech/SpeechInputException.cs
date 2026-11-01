using System.Net;

namespace TGVoiceMessagesEditText.Services.Speech;

public sealed class SpeechInputException(HttpStatusCode statusCode, string detail) : Exception(detail)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
