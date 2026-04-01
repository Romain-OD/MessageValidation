namespace MessageValidation;

/// <summary>
/// Represents a single validation error for a message property.
/// Returned inside <see cref="MessageValidationResult.Errors"/> when validation fails.
/// </summary>
/// <param name="PropertyName">
/// The name of the property that failed validation (e.g., <c>"SensorId"</c>).
/// May be empty when the error applies to the message as a whole.
/// </param>
/// <param name="ErrorMessage">
/// A human-readable description of the validation failure
/// (e.g., <c>"SensorId is required."</c>).
/// </param>
public sealed record MessageValidationError(string PropertyName, string ErrorMessage);
