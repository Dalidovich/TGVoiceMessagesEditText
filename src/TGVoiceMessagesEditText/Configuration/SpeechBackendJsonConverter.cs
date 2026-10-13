using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGVoiceMessagesEditText.Configuration;

public class SpeechBackendJsonConverter : JsonConverter<SpeechBackend>
{
    private const SpeechBackend Fallback = SpeechBackend.Vulkan;

    public override SpeechBackend Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? raw;
        if (reader.TokenType == JsonTokenType.String)
        {
            raw = reader.GetString();
        }
        else
        {
            using var document = JsonDocument.ParseValue(ref reader);
            raw = document.RootElement.GetRawText();
        }

        if (Enum.TryParse<SpeechBackend>(raw, true, out var backend) && Enum.IsDefined(backend))
        {
            return backend;
        }

        Console.Error.WriteLine(
            $"Unknown Speech.Backend '{raw}' in settings.json, supported values: {string.Join(", ", Enum.GetNames<SpeechBackend>())}. {Fallback} is used.");
        return Fallback;
    }

    public override void Write(Utf8JsonWriter writer, SpeechBackend value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
