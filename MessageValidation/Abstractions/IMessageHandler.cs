namespace MessageValidation;

/// <summary>
/// Defines a handler for a validated message.
/// Only invoked when validation passes.
/// </summary>
/// <typeparam name="TMessage">The message type to handle.</typeparam>
public interface IMessageHandler<in TMessage> where TMessage : class
{
    Task HandleAsync(TMessage message, MessageContext context, CancellationToken ct = default);
}
