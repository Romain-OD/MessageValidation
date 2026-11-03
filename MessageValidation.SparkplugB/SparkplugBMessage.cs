using Google.Protobuf;
using MessageValidation.SparkplugB.Protobuf;

namespace MessageValidation.SparkplugB;

/// <summary>
/// A Sparkplug B message: the parsed topic plus the decoded protobuf <see cref="Payload"/>.
/// Produced by <see cref="SparkplugBMiddleware"/> and validated by <see cref="SparkplugBValidator"/>.
/// </summary>
/// <remarks>
/// When the topic or payload cannot be parsed, <see cref="ParseError"/> is set and the
/// validator reports it, so handlers only ever see well-formed messages.
/// </remarks>
public sealed record SparkplugBMessage(SparkplugTopic Topic, Payload Payload)
{
    /// <summary>Topic or protobuf decoding error, or <see langword="null"/> when parsing succeeded.</summary>
    public string? ParseError { get; init; }

    /// <summary>Parses a raw MQTT topic and payload. Never throws for malformed input.</summary>
    public static SparkplugBMessage Parse(string topic, byte[] payload)
    {
        if (!SparkplugTopic.TryParse(topic, out var parsed, out var error))
            return new(Unparsed, new Payload()) { ParseError = error };

        try
        {
            return new SparkplugBMessage(parsed, Payload.Parser.ParseFrom(payload));
        }
        catch (InvalidProtocolBufferException ex)
        {
            return new(parsed, new Payload()) { ParseError = $"Payload is not a valid Sparkplug B protobuf: {ex.Message}" };
        }
    }

    private static readonly SparkplugTopic Unparsed = new("", SparkplugMessageType.Unknown, "");
}
