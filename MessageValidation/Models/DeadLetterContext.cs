namespace MessageValidation;

/// <summary>
/// Contains all the information needed to dead-letter a message
/// that failed validation.
/// </summary>
public sealed class DeadLetterContext
{
    /// <summary>
    /// The computed dead-letter destination (e.g. "$dead-letter/sensors/temperature").
    /// Built from <see cref="MessageValidationOptions.DeadLetterPrefix"/> + <see cref="MessageContext.Source"/>.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// The original message context that failed validation.
    /// </summary>
    public required MessageContext OriginalContext { get; init; }

    /// <summary>
    /// The validation result containing the errors that caused the dead-letter.
    /// </summary>
    public required MessageValidationResult ValidationResult { get; init; }

    /// <summary>
    /// UTC timestamp when the dead-letter decision was made.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
