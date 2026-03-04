# MessageValidation.FluentValidation

[FluentValidation](https://docs.fluentvalidation.net/) adapter for the [MessageValidation](../MessageValidation/README.md) pipeline — automatically bridge your existing FluentValidation validators into the protocol-agnostic message validation pipeline.

## Installation

```bash
dotnet add package MessageValidation.FluentValidation
```

## Quick Start

### 1. Define your message

```csharp
public class TemperatureReading
{
    public string SensorId { get; set; } = "";
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 2. Create a FluentValidation validator

Write a standard FluentValidation `AbstractValidator<T>` — no special base class required:

```csharp
using FluentValidation;

public class TemperatureReadingValidator : AbstractValidator<TemperatureReading>
{
    public TemperatureReadingValidator()
    {
        RuleFor(x => x.SensorId).NotEmpty();
        RuleFor(x => x.Value).InclusiveBetween(-50, 150);
        RuleFor(x => x.Timestamp).LessThanOrEqualTo(DateTime.UtcNow);
    }
}
```

### 3. Register services

```csharp
using MessageValidation;
using MessageValidation.FluentValidation;

builder.Services.AddMessageValidation(options =>
{
    options.MapSource<TemperatureReading>("sensors/+/temperature");
    options.DefaultFailureBehavior = FailureBehavior.Log;
});

// Scan the assembly for all AbstractValidator<T> implementations
// and register the FluentValidation adapter as the IMessageValidator<T> bridge.
builder.Services.AddMessageFluentValidation(typeof(Program).Assembly);

builder.Services.AddMessageDeserializer<JsonMessageDeserializer>();
builder.Services.AddMessageHandler<TemperatureReading, TemperatureHandler>();
```

That's it. The pipeline will now use your `TemperatureReadingValidator` automatically when a `TemperatureReading` message arrives.

## How It Works

`AddMessageFluentValidation` does two things:

1. **Scans assemblies** for all `AbstractValidator<T>` implementations and registers them in the DI container (via `FluentValidation.DependencyInjectionExtensions`).
2. **Registers an open-generic adapter** (`FluentValidationMessageValidator<T>`) that implements `IMessageValidator<T>` by delegating to FluentValidation's `IValidator<T>`.

```
IMessageValidator<T>
    └── FluentValidationMessageValidator<T>
            └── IValidator<T>  (your AbstractValidator<T>)
```

## API Reference

### `AddMessageFluentValidation(params Assembly[] assemblies)`

| Parameter | Description |
|---|---|
| `assemblies` | Assemblies to scan for `AbstractValidator<T>` implementations. If none are provided, the calling assembly is scanned. |

```csharp
// Scan specific assemblies
builder.Services.AddMessageFluentValidation(
    typeof(TemperatureReadingValidator).Assembly,
    typeof(DeviceHeartbeatValidator).Assembly);

// Or scan the calling assembly (default)
builder.Services.AddMessageFluentValidation();
```

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) core package
- [FluentValidation](https://www.nuget.org/packages/FluentValidation) 11+

## License

[MIT](../LICENSE)
