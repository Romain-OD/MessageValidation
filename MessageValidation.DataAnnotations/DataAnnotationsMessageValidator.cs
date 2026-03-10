using System.ComponentModel.DataAnnotations;

namespace MessageValidation.DataAnnotations;

/// <summary>
/// Bridges <see cref="System.ComponentModel.DataAnnotations"/> validation into the
/// <see cref="IMessageValidator{TMessage}"/> contract used by the MessageValidation pipeline.
/// </summary>
/// <typeparam name="TMessage">The message type to validate.</typeparam>
public sealed class DataAnnotationsMessageValidator<TMessage> : IMessageValidator<TMessage>
    where TMessage : class
{
    /// <inheritdoc />
    public Task<MessageValidationResult> ValidateAsync(TMessage message, CancellationToken ct = default)
    {
        var context = new ValidationContext(message);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(message, context, results, validateAllProperties: true);

        return Task.FromResult(isValid
            ? MessageValidationResult.Success()
            : MessageValidationResult.Failure(
                results.Select(r => new MessageValidationError(
                    r.MemberNames.FirstOrDefault() ?? string.Empty,
                    r.ErrorMessage ?? "Validation failed."))));
    }
}
