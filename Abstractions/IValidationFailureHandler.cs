namespace MessageValidation;

/// <summary>
/// Handles validation failures. Register a custom implementation
/// when <see cref="FailureBehavior.Custom"/> is configured.
/// </summary>
public interface IValidationFailureHandler
{
    Task HandleAsync(MessageValidationResult result, MessageContext context, CancellationToken ct = default);
}
