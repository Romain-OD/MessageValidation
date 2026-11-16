namespace MessageValidation.Examples;

/// <summary>Message shared by the core and transport examples.</summary>
public sealed record TemperatureReading(string SensorId, double Value)
{
    /// <summary>Two valid readings and one invalid one: every transport example sends these.</summary>
    public static readonly TemperatureReading[] Samples =
    [
        new("kitchen-01", 21.5),
        new("garage-02", 18.0),
        new("", 999),
    ];
}

/// <summary>Hand-written validator: the core package needs no validation library.</summary>
public sealed class TemperatureReadingValidator : IMessageValidator<TemperatureReading>
{
    public Task<MessageValidationResult> ValidateAsync(TemperatureReading message, CancellationToken ct = default)
    {
        List<MessageValidationError> errors = [];

        if (string.IsNullOrWhiteSpace(message.SensorId))
            errors.Add(new(nameof(message.SensorId), "SensorId is required."));

        if (message.Value is < -50 or > 150)
            errors.Add(new(nameof(message.Value), "Value must be between -50 and 150."));

        return Task.FromResult(errors.Count == 0 ? MessageValidationResult.Success() : MessageValidationResult.Failure(errors));
    }
}
