namespace MessageValidation;

/// <summary>
/// Determines how the <see cref="IMessageValidationPipeline"/> handles messages
/// that fail validation. Set via <see cref="MessageValidationOptions.DefaultFailureBehavior"/>.
/// </summary>
public enum FailureBehavior
{
    /// <summary>
    /// Log a warning with the validation errors and continue.
    /// This is the default behavior.
    /// </summary>
    Log,

    /// <summary>
    /// Route the failed message to a dead-letter destination via the registered
    /// <see cref="IDeadLetterHandler"/>. The destination is built from
    /// <see cref="MessageValidationOptions.DeadLetterPrefix"/> + <see cref="MessageContext.Source"/>.
    /// </summary>
    DeadLetter,

    /// <summary>
    /// Silently discard the message without logging or any side effect.
    /// </summary>
    Skip,

    /// <summary>
    /// Throw a <see cref="MessageValidationException"/> containing the
    /// <see cref="MessageValidationResult"/> with all validation errors.
    /// </summary>
    ThrowException,

    /// <summary>
    /// Delegate failure handling to a custom <see cref="IValidationFailureHandler"/>
    /// registered in the DI container.
    /// </summary>
    Custom
}
