namespace MessageValidation;

/// <summary>
/// Defines the core message validation pipeline contract.
/// Accepts a protocol-agnostic <see cref="MessageContext"/>, deserializes the raw payload,
/// validates the resulting object, and dispatches it to the registered <see cref="IMessageHandler{TMessage}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the central entry point of the <c>MessageValidation</c> library.
/// Transport adapters (RabbitMQ, MQTT, Kafka, Azure Service Bus, etc.) call
/// <see cref="ProcessAsync"/> for every incoming message.
/// </para>
/// <para>
/// Register the default implementation via
/// <see cref="ServiceCollectionExtensions.AddMessageValidation"/> in your DI container.
/// </para>
/// <para><strong>Pipeline flow:</strong></para>
/// <list type="number">
///   <item><description>Resolve the target CLR type from <see cref="MessageContext.Source"/> using <see cref="MessageValidationOptions.TryResolveMessageType"/>.</description></item>
///   <item><description>Deserialize <see cref="MessageContext.RawPayload"/> via the registered <see cref="IMessageDeserializer"/>.</description></item>
///   <item><description>Validate the object with the registered <see cref="IMessageValidator{TMessage}"/>.</description></item>
///   <item><description>On success, dispatch to <see cref="IMessageHandler{TMessage}"/>. On failure, apply the configured <see cref="FailureBehavior"/>.</description></item>
/// </list>
/// </remarks>
public interface IMessageValidationPipeline
{
    /// <summary>
    /// Deserializes, validates, and dispatches a single incoming message.
    /// </summary>
    /// <param name="context">
    /// The protocol-agnostic message context containing the source identifier,
    /// raw payload bytes, and optional transport metadata.
    /// </param>
    /// <param name="ct">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> that completes when the message has been fully processed.</returns>
    Task ProcessAsync(MessageContext context, CancellationToken ct = default);
}
