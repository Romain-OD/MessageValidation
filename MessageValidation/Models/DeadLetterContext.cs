namespace MessageValidation;

/// <summary>
/// Contains all the information needed to dead-letter a message
/// that failed validation. Passed to <see cref="IDeadLetterHandler.HandleAsync"/>
/// when <see cref="FailureBehavior.DeadLetter"/> is configured.
/// </summary>
public sealed class DeadLetterContext
{
    /// <summary>
    /// The computed dead-letter destination (e.g., <c>"$dead-letter/sensors/temperature"</c>).
    /// Built from <see cref="MessageValidationOptions.DeadLetterPrefix"/> + <see cref="MessageContext.Source"/>.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// The original <see cref="MessageContext"/> of the message that failed validation,
    /// including its <see cref="MessageContext.RawPayload"/> and <see cref="MessageContext.Metadata"/>.
    /// </summary>
    public required MessageContext OriginalContext { get; init; }

    /// <summary>
    /// The <see cref="MessageValidationResult"/> containing the errors that caused
    /// the message to be dead-lettered.
    /// </summary>
    public required MessageValidationResult ValidationResult { get; init; }

    /// <summary>
    /// UTC timestamp when the dead-letter decision was made by the pipeline.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
