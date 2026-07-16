using Immediate.Validations.Shared;

namespace Example.ImmediateValidation;

[Validate]
public partial record TemperatureReading : IValidationTarget<TemperatureReading>
{
    public required string SensorId { get; init; }

    public required double Value { get; init; }

    public required DateTime Timestamp { get; init; }

    private static void AdditionalValidations(
        ValidationResult errors,
        TemperatureReading target)
    {
        if (string.IsNullOrWhiteSpace(target.SensorId))
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(SensorId),
                ErrorMessage = "SensorId is required.",
            });
        }

        if (target.Value is < -50 or > 150)
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(Value),
                ErrorMessage = "Value must be between -50 and 150.",
            });
        }

        if (target.Timestamp == default)
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(Timestamp),
                ErrorMessage = "Timestamp is required.",
            });
        }
    }
}

[Validate]
public partial record DeviceHeartbeat : IValidationTarget<DeviceHeartbeat>
{
    public required string DeviceId { get; init; }

    public required string Status { get; init; }

    private static void AdditionalValidations(
        ValidationResult errors,
        DeviceHeartbeat target)
    {
        if (string.IsNullOrWhiteSpace(target.DeviceId))
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(DeviceId),
                ErrorMessage = "DeviceId is required.",
            });
        }

        if (target.Status is not ("online" or "offline"))
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(Status),
                ErrorMessage = "Status must be 'online' or 'offline'.",
            });
        }
    }
}

