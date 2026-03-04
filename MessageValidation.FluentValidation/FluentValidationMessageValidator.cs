using FluentValidation;

namespace MessageValidation.FluentValidation;

/// <summary>
/// Bridges a FluentValidation <see cref="IValidator{T}"/> into the
/// <see cref="IMessageValidator{TMessage}"/> contract used by the MessageValidation pipeline.
/// </summary>
/// <typeparam name="TMessage">The message type to validate.</typeparam>
public sealed class FluentValidationMessageValidator<TMessage>(
    IValidator<TMessage> validator) : IMessageValidator<TMessage>
    where TMessage : class
{
    /// <inheritdoc />
    public async Task<MessageValidationResult> ValidateAsync(TMessage message, CancellationToken ct = default)
    {
        var result = await validator.ValidateAsync(message, ct);

        return result.IsValid
            ? MessageValidationResult.Success()
            : MessageValidationResult.Failure(
                result.Errors.Select(e => new MessageValidationError(e.PropertyName, e.ErrorMessage)));
    }
}
