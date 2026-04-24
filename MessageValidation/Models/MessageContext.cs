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

    /// <summary>
    /// The CLR type resolved from <see cref="Source"/>. Populated by the type-resolution
    /// middleware and consumed by subsequent middleware (deserialization, validation,
    /// dispatch).
    /// </summary>
    public Type? MessageType { get; set; }

    /// <summary>
    /// The deserialized message instance. Populated by the deserialization middleware.
    /// </summary>
    public object? Message { get; set; }

    /// <summary>
    /// The outcome of the most recent validation step, or <see langword="null"/> when
    /// validation has not yet run or no validator is registered for <see cref="MessageType"/>.
    /// </summary>
    public MessageValidationResult? ValidationResult { get; set; }

    /// <summary>
    /// The per-message (scoped) <see cref="IServiceProvider"/>. Populated by
    /// <see cref="IMessageValidationPipeline"/> before dispatching to middleware.
    /// </summary>
    public IServiceProvider? Services { get; set; }

    /// <summary>
    /// Extensibility bag for custom middleware to share state across the pipeline.
    /// </summary>
    public IDictionary<string, object?> Items { get; } = new Dictionary<string, object?>();
}
