namespace MessageValidation;

/// <summary>
/// Protocol-agnostic context for an incoming message.
/// Every transport adapter (RabbitMQ, MQTT, Kafka, etc.) creates a
/// <see cref="MessageContext"/> and passes it to <see cref="IMessageValidationPipeline.ProcessAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Source"/> property is used to resolve the target CLR type via
/// <see cref="MessageValidationOptions.TryResolveMessageType"/>.
/// Transport-specific details (headers, QoS, partition, etc.) belong in <see cref="Metadata"/>.
/// </para>
/// </remarks>
public sealed class MessageContext
{
    /// <summary>
    /// The message source identifier — topic name, queue name, routing key, or any
    /// string that uniquely identifies the channel. Used by the pipeline to resolve
    /// the target message type via <see cref="MessageValidationOptions.MapSource{TMessage}"/>.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The raw message payload bytes as received from the transport.
    /// Passed to the registered <see cref="IMessageDeserializer"/> for deserialization.
    /// </summary>
    public required byte[] RawPayload { get; init; }

    /// <summary>
    /// Protocol-specific metadata (headers, properties, QoS level, partition, offset, etc.).
    /// Populated by the transport adapter; available in handlers and failure handlers.
    /// </summary>
    public IDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
