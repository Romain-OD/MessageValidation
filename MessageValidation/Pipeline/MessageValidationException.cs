namespace MessageValidation;

/// <summary>
/// Exception thrown by the <see cref="IMessageValidationPipeline"/> when
/// <see cref="FailureBehavior.ThrowException"/> is configured and message validation fails.
/// Contains the full <see cref="ValidationResult"/> with all errors.
/// </summary>
public sealed class MessageValidationException : Exception
{
    /// <summary>
    /// Gets the <see cref="MessageValidationResult"/> that caused this exception,
    /// including all <see cref="MessageValidationResult.Errors"/>.
    /// </summary>
    public MessageValidationResult ValidationResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageValidationException"/> class
    /// with a formatted error message built from the validation errors.
    /// </summary>
    /// <param name="result">
    /// The <see cref="MessageValidationResult"/> containing the validation errors.
    /// </param>
    public MessageValidationException(MessageValidationResult result)
        : base($"Message validation failed: {string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))}")
    {
        ValidationResult = result;
    }
}
