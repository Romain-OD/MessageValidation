namespace MessageValidation;

/// <summary>
/// Represents a single validation error for a message property.
/// </summary>
public sealed record MessageValidationError(string PropertyName, string ErrorMessage);
