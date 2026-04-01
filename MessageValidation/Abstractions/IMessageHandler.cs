namespace MessageValidation;

/// <summary>
/// Defines a handler for a validated message of type <typeparamref name="TMessage"/>.
/// Only invoked when the <see cref="IMessageValidator{TMessage}"/> reports success.
/// </summary>
/// <typeparam name="TMessage">The deserialized message type to handle.</typeparam>
/// <remarks>
/// <para>
/// Implement this interface for each message type your application consumes.
/// The handler is resolved from the DI container per scope, so you can inject
/// scoped dependencies (e.g., <c>DbContext</c>, HTTP clients).
/// </para>
/// <para>
/// Register with:
/// <c>services.AddMessageHandler&lt;MyMessage, MyMessageHandler&gt;()</c>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderCreatedHandler : IMessageHandler&lt;OrderCreated&gt;
/// {
///     public Task HandleAsync(OrderCreated message, MessageContext context, CancellationToken ct)
///     {
///         Console.WriteLine($"Order {message.OrderId} received from {context.Source}");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public interface IMessageHandler<in TMessage> where TMessage : class
{
    /// <summary>
    /// Processes a validated message.
    /// </summary>
    /// <param name="message">The deserialized and validated message instance.</param>
    /// <param name="context">
    /// The original <see cref="MessageContext"/> containing the source, raw payload, and transport metadata.
    /// </param>
    /// <param name="ct">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> that completes when processing is finished.</returns>
    Task HandleAsync(TMessage message, MessageContext context, CancellationToken ct = default);
}
