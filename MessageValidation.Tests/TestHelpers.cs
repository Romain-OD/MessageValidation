using System.Text.Json;

namespace MessageValidation.Tests;

public static class TestHelpers
{
    public static byte[] ToPayload<T>(T obj) =>
        JsonSerializer.SerializeToUtf8Bytes(obj);

    public static MessageContext CreateContext(string source, byte[] payload) =>
        new() { Source = source, RawPayload = payload };

    public static MessageContext CreateContext<T>(string source, T obj) =>
        CreateContext(source, ToPayload(obj));
}

public class JsonTestDeserializer : IMessageDeserializer
{
    public object Deserialize(byte[] payload, Type targetType) =>
        JsonSerializer.Deserialize(payload, targetType)
        ?? throw new InvalidOperationException($"Failed to deserialize to {targetType.Name}");
}

public class TestMessage
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}
