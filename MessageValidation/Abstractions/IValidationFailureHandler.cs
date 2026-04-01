namespace MessageValidation;

/// <summary>
/// Handles validation failures with custom application-specific logic.
/// Invoked by the pipeline when <see cref="FailureBehavior.Custom"/> is configured.
/// </summary>
/// <remarks>
/// <para>
/// Use this when the built-in failure behaviors (<see cref="FailureBehavior.Log"/>,
/// <see cref="FailureBehavior.Skip"/>, <see cref="FailureBehavior.DeadLetter"/>,
/// <see cref="FailureBehavior.ThrowException"/>) are not sufficient.
/// </para>
/// <para>
/// Register with:
/// <c>services.AddValidationFailureHandler&lt;MyFailureHandler&gt;()</c>
/// </para>
/// </remarks>
public interface IValidationFailureHandler
{
    /// <summary>
    /// Called when validation fails and <see cref="FailureBehavior.Custom"/> is active.
    /// </summary>
    /// <param name="result">
    /// The <see cref="MessageValidationResult"/> containing the validation errors.
    /// </param>
    /// <param name="context">
    /// The original <see cref="MessageContext"/> for the failed message.
    /// </param>
    /// <param name="ct">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Task"/> that completes when failure handling is finished.</returns>
    Task HandleAsync(MessageValidationResult result, MessageContext context, CancellationToken ct = default);
}
