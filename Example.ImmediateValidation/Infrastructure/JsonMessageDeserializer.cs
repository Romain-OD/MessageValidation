using System.Text.Json;
using MessageValidation;

namespace Example.ImmediateValidation;

public sealed class JsonMessageDeserializer : IMessageDeserializer
{
    public object Deserialize(byte[] payload, Type targetType) =>
        JsonSerializer.Deserialize(payload, targetType)
        ?? throw new InvalidOperationException($"Failed to deserialize to {targetType.Name}");
}
