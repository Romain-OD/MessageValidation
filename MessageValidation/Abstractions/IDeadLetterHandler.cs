namespace MessageValidation;

/// <summary>
/// Handles dead-lettering of messages that failed validation.
/// Invoked by the pipeline when <see cref="FailureBehavior.DeadLetter"/> is configured.
/// </summary>
/// <remarks>
/// <para>
/// Implement a transport-specific dead-letter handler to republish the failed
/// message to a dead-letter queue, topic, or storage location.
/// The <see cref="DeadLetterContext.Destination"/> is built from
/// <see cref="MessageValidationOptions.DeadLetterPrefix"/> + <see cref="MessageContext.Source"/>.
/// </para>
/// <para>
/// Register with:
/// <c>services.AddDeadLetterHandler&lt;MyDeadLetterHandler&gt;()</c>
/// </para>
/// </remarks>
public interface IDeadLetterHandler
{
    /// <summary>
    /// Publishes or persists the failed message to the dead-letter destination.
    /// </summary>
    /// <param name="deadLetterContext">
    /// Contains the computed <see cref="DeadLetterContext.Destination"/>,
    /// the <see cref="DeadLetterContext.OriginalContext"/>, and the
    /// <see cref="DeadLetterContext.ValidationResult"/> with error details.
    /// </param>
    /// <param name="ct">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> that completes when dead-lettering is finished.</returns>
    Task HandleAsync(DeadLetterContext deadLetterContext, CancellationToken ct = default);
}
