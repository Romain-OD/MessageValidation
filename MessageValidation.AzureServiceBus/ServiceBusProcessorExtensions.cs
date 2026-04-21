using Azure.Messaging.ServiceBus;

namespace MessageValidation.AzureServiceBus;

/// <summary>
/// Extension methods for integrating the MessageValidation pipeline with an
/// <see cref="ServiceBusProcessor"/> or <see cref="ServiceBusSessionProcessor"/>.
/// </summary>
public static class ServiceBusProcessorExtensions
{
    /// <summary>
    /// Hooks the <see cref="IMessageValidationPipeline"/> into the processor's
    /// <see cref="ServiceBusProcessor.ProcessMessageAsync"/> event so that every
    /// received message is automatically deserialized, validated, and dispatched.
    /// Also wires a minimal <see cref="ServiceBusProcessor.ProcessErrorAsync"/>
    /// handler (required by the SDK).
    /// </summary>
    /// <param name="processor">The Azure Service Bus processor.</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <param name="onError">
    /// Optional error callback. When omitted, errors are swallowed silently
    /// (the default <see cref="ServiceBusProcessor.ProcessErrorAsync"/> handler
    /// still needs to be registered for the processor to start).
    /// </param>
    /// <returns>The same <see cref="ServiceBusProcessor"/> for chaining.</returns>
    public static ServiceBusProcessor UseMessageValidation(
        this ServiceBusProcessor processor,
        IMessageValidationPipeline pipeline,
        Func<ProcessErrorEventArgs, Task>? onError = null)
    {
        var entityPath = processor.EntityPath;

        processor.ProcessMessageAsync += args =>
            HandleMessageAsync(args.Message, entityPath, pipeline, args.CancellationToken);

        processor.ProcessErrorAsync += onError ?? (_ => Task.CompletedTask);

        return processor;
    }

    /// <summary>
    /// Hooks the <see cref="IMessageValidationPipeline"/> into the session processor's
    /// <see cref="ServiceBusSessionProcessor.ProcessMessageAsync"/> event so that every
    /// received message is automatically deserialized, validated, and dispatched.
    /// Also wires a minimal <see cref="ServiceBusSessionProcessor.ProcessErrorAsync"/>
    /// handler (required by the SDK).
    /// </summary>
    /// <param name="processor">The Azure Service Bus session processor.</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <param name="onError">Optional error callback.</param>
    /// <returns>The same <see cref="ServiceBusSessionProcessor"/> for chaining.</returns>
    public static ServiceBusSessionProcessor UseMessageValidation(
        this ServiceBusSessionProcessor processor,
        IMessageValidationPipeline pipeline,
        Func<ProcessErrorEventArgs, Task>? onError = null)
    {
        var entityPath = processor.EntityPath;

        processor.ProcessMessageAsync += args =>
            HandleMessageAsync(args.Message, entityPath, pipeline, args.CancellationToken);

        processor.ProcessErrorAsync += onError ?? (_ => Task.CompletedTask);

        return processor;
    }

    /// <summary>
    /// Builds a protocol-agnostic <see cref="MessageContext"/> from an
    /// Azure Service Bus <see cref="ServiceBusReceivedMessage"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="MessageContext.Source"/> is set to the message's
    /// <see cref="ServiceBusReceivedMessage.Subject"/> when present,
    /// otherwise falls back to <paramref name="entityPath"/>.
    /// This lets you fan-in multiple message types on a single queue/topic
    /// and route them via <c>MapSource&lt;T&gt;("subject-name")</c>.
    /// </remarks>
    /// <param name="message">The received Service Bus message.</param>
    /// <param name="entityPath">The queue, topic, or subscription path.</param>
    /// <returns>A fully populated <see cref="MessageContext"/>.</returns>
    public static MessageContext CreateContext(ServiceBusReceivedMessage message, string entityPath)
    {
        ArgumentNullException.ThrowIfNull(message);

        var source = string.IsNullOrEmpty(message.Subject) ? entityPath : message.Subject;

        var metadata = new Dictionary<string, object>
        {
            ["servicebus.entityPath"] = entityPath,
            ["servicebus.messageId"] = message.MessageId ?? string.Empty,
            ["servicebus.subject"] = message.Subject ?? string.Empty,
            ["servicebus.correlationId"] = message.CorrelationId ?? string.Empty,
            ["servicebus.contentType"] = message.ContentType ?? string.Empty,
            ["servicebus.sessionId"] = message.SessionId ?? string.Empty,
            ["servicebus.enqueuedTime"] = message.EnqueuedTime.UtcDateTime,
            ["servicebus.deliveryCount"] = message.DeliveryCount,
            ["servicebus.applicationProperties"] = message.ApplicationProperties
        };

        return new MessageContext
        {
            Source = source,
            RawPayload = message.Body?.ToArray() ?? [],
            Metadata = metadata
        };
    }

    private static Task HandleMessageAsync(
        ServiceBusReceivedMessage message,
        string entityPath,
        IMessageValidationPipeline pipeline,
        CancellationToken ct)
    {
        var context = CreateContext(message, entityPath);
        return pipeline.ProcessAsync(context, ct);
    }
}
