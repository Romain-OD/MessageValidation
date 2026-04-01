namespace MessageValidation;

/// <summary>
/// Defines a validator for a specific message type.
/// Implement this interface directly for custom validation logic,
/// or use an adapter package to bridge an existing validation framework.
/// </summary>
/// <typeparam name="TMessage">The deserialized message type to validate.</typeparam>
/// <remarks>
/// <para>
/// The pipeline resolves <c>IMessageValidator&lt;TMessage&gt;</c> from the DI container
/// for every incoming message. If no validator is registered the message is
/// passed directly to the <see cref="IMessageHandler{TMessage}"/>.
/// </para>
/// <para><strong>Built-in adapter packages:</strong></para>
/// <list type="bullet">
///   <item><description><c>MessageValidation.FluentValidation</c> — bridges <c>FluentValidation.IValidator&lt;T&gt;</c>.</description></item>
///   <item><description><c>MessageValidation.DataAnnotations</c> — bridges <c>System.ComponentModel.DataAnnotations</c> attributes.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class OrderCreatedValidator : IMessageValidator&lt;OrderCreated&gt;
/// {
///     public Task&lt;MessageValidationResult&gt; ValidateAsync(OrderCreated message, CancellationToken ct)
///     {
///         var errors = new List&lt;MessageValidationError&gt;();
///         if (message.OrderId == Guid.Empty)
///             errors.Add(new("OrderId", "OrderId is required."));
///         return Task.FromResult(errors.Count == 0
///             ? MessageValidationResult.Success()
///             : MessageValidationResult.Failure(errors));
///     }
/// }
/// </code>
/// </example>
public interface IMessageValidator<in TMessage> where TMessage : class
{
    /// <summary>
    /// Validates the deserialized <paramref name="message"/> and returns a
    /// <see cref="MessageValidationResult"/> indicating success or the list of errors.
    /// </summary>
    /// <param name="message">The deserialized message instance to validate.</param>
    /// <param name="ct">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A <see cref="MessageValidationResult"/> where <see cref="MessageValidationResult.IsValid"/>
    /// is <see langword="true"/> when validation passes, or <see langword="false"/> with
    /// populated <see cref="MessageValidationResult.Errors"/> when it fails.
    /// </returns>
    Task<MessageValidationResult> ValidateAsync(TMessage message, CancellationToken ct = default);
}
