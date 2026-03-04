namespace MessageValidation;

/// <summary>
/// Represents the outcome of a message validation operation.
/// </summary>
public sealed class MessageValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<MessageValidationError> Errors { get; init; } = [];

    public static MessageValidationResult Success() => new() { Errors = [] };

    public static MessageValidationResult Failure(IEnumerable<MessageValidationError> errors) =>
        new() { Errors = errors.ToList().AsReadOnly() };
}
