using System.Text.Json;

namespace Quiz.Shared.Networking;

public sealed class MessageEnvelope
{
    public required string Type { get; init; }
    public string? RequestId { get; init; }
    public JsonElement Payload { get; init; }

    public static MessageEnvelope Create<T>(string type, T payload, string? requestId = null)
    {
        // Serializa payload para JsonElement (evita generics no transporte)
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions.Default);
        var doc = JsonDocument.Parse(bytes);
        return new MessageEnvelope
        {
            Type = type,
            RequestId = requestId,
            Payload = doc.RootElement.Clone()
        };
    }

    public T? PayloadAs<T>()
        => Payload.ValueKind == JsonValueKind.Undefined || Payload.ValueKind == JsonValueKind.Null
            ? default
            : Payload.Deserialize<T>(JsonOptions.Default);
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
