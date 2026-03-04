namespace MessageValidation;

/// <summary>
/// Defines a validator for a specific message type.
/// Implement this interface directly for custom validation,
/// or use an adapter package (e.g., MessageValidation.FluentValidation).
/// </summary>
/// <typeparam name="TMessage">The message type to validate.</typeparam>
public interface IMessageValidator<in TMessage> where TMessage : class
{
    Task<MessageValidationResult> ValidateAsync(TMessage message, CancellationToken ct = default);
}
