namespace MessageValidation;

/// <summary>
/// Handles dead-lettering of messages that failed validation.
/// Register a transport-specific or custom implementation when
/// <see cref="FailureBehavior.DeadLetter"/> is configured.
/// </summary>
public interface IDeadLetterHandler
{
    Task HandleAsync(DeadLetterContext deadLetterContext, CancellationToken ct = default);
}
