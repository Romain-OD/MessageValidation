namespace MessageValidation;

/// <summary>
/// Represents the outcome of a message validation operation.
/// Returned by <see cref="IMessageValidator{TMessage}.ValidateAsync"/> and inspected
/// by the <see cref="IMessageValidationPipeline"/> to decide whether to dispatch
/// the message or trigger the configured <see cref="FailureBehavior"/>.
/// </summary>
public sealed class MessageValidationResult
{
    /// <summary>
    /// Gets a value indicating whether validation passed (<see langword="true"/>)
    /// or failed (<see langword="false"/>). Equivalent to <c>Errors.Count == 0</c>.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the list of validation errors. Empty when <see cref="IsValid"/> is <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<MessageValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A <see cref="MessageValidationResult"/> where <see cref="IsValid"/> is <see langword="true"/>.</returns>
    public static MessageValidationResult Success() => new() { Errors = [] };

    /// <summary>
    /// Creates a failed validation result from the specified errors.
    /// </summary>
    /// <param name="errors">One or more <see cref="MessageValidationError"/> instances describing the failures.</param>
    /// <returns>A <see cref="MessageValidationResult"/> where <see cref="IsValid"/> is <see langword="false"/>.</returns>
    public static MessageValidationResult Failure(IEnumerable<MessageValidationError> errors) =>
        new() { Errors = errors.ToList().AsReadOnly() };
}
