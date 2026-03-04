namespace MessageValidation;

/// <summary>
/// Thrown when <see cref="FailureBehavior.ThrowException"/> is configured and validation fails.
/// </summary>
public sealed class MessageValidationException : Exception
{
    public MessageValidationResult ValidationResult { get; }

    public MessageValidationException(MessageValidationResult result)
        : base($"Message validation failed: {string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))}")
    {
        ValidationResult = result;
    }
}
