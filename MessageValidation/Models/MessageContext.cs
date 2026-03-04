namespace MessageValidation;

/// <summary>
/// Protocol-agnostic context for an incoming message.
/// </summary>
public sealed class MessageContext
{
    /// <summary>
    /// The message source identifier — topic, queue name, routing key, etc.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The raw message payload bytes.
    /// </summary>
    public required byte[] RawPayload { get; init; }

    /// <summary>
    /// Protocol-specific metadata (headers, properties, QoS, etc.).
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
