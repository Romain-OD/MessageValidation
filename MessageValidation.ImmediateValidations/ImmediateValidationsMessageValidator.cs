using Immediate.Validations.Shared;

namespace MessageValidation.ImmediateValidations;

/// <summary>
/// Bridges an <see cref="IValidationTarget{T}"/> (Immediate.Validations source-generated validator)
/// into the <see cref="IMessageValidator{TMessage}"/> contract used by the MessageValidation pipeline.
/// </summary>
/// <typeparam name="TMessage">
/// The message type to validate. Must be annotated with <c>[Validate]</c> and implement
/// <see cref="IValidationTarget{T}"/> so the Immediate.Validations source generator
/// emits the static <c>Validate</c> method.
/// </typeparam>
/// <remarks>
/// Validation runs through the source-generated, AOT-friendly, zero-reflection
/// <c>TMessage.Validate(message)</c> static call. No DI lookup is performed per validation
/// — the adapter only translates the resulting <see cref="ValidationResult"/> into a
/// <see cref="MessageValidationResult"/>.
/// </remarks>
public sealed class ImmediateValidationsMessageValidator<TMessage> : IMessageValidator<TMessage>
    where TMessage : class, IValidationTarget<TMessage>
{
    /// <inheritdoc />
    public Task<MessageValidationResult> ValidateAsync(TMessage message, CancellationToken ct = default)
    {
        var errors = TMessage.Validate(message)
            .Select(e => new MessageValidationError(e.PropertyName, e.ErrorMessage))
            .ToList();

        return Task.FromResult(errors.Count == 0
            ? MessageValidationResult.Success()
            : MessageValidationResult.Failure(errors));
    }
}
