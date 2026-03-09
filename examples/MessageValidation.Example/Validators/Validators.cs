using FluentValidation;

namespace MessageValidation.Example;

public class TemperatureReadingValidator : AbstractValidator<TemperatureReading>
{
    public TemperatureReadingValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty()
            .WithMessage("SensorId is required.");

        RuleFor(x => x.Value)
            .InclusiveBetween(-50, 150)
            .WithMessage("Value must be between -50 and 150.");

        RuleFor(x => x.Timestamp)
            .NotEqual(default(DateTime))
            .WithMessage("Timestamp is required.");
    }
}

public class DeviceHeartbeatValidator : AbstractValidator<DeviceHeartbeat>
{
    public DeviceHeartbeatValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("DeviceId is required.");

        RuleFor(x => x.Status)
            .Must(s => s is "online" or "offline")
            .WithMessage("Status must be 'online' or 'offline'.");
    }
}
