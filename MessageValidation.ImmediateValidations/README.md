# MessageValidation.ImmediateValidations

An **[Immediate.Validations](https://github.com/ImmediatePlatform/Immediate.Validations) adapter** for [MessageValidation](https://github.com/Romain-OD/MessageValidation) — bridge **source-generated, AOT-friendly, zero-reflection** validators into the MessageValidation pipeline.

## Installation

```bash
dotnet add package MessageValidation.ImmediateValidations
```

## Quick Start

### 1. Decorate your message

Mark the message type with `[Validate]`, make it `partial`, and implement `IValidationTarget<T>`. The Immediate.Validations source generator emits the `Validate` method at compile time.

```csharp
using Immediate.Validations.Shared;

[Validate]
public partial record TemperatureReading : IValidationTarget<TemperatureReading>
{
    [NotEmpty]
    public required string SensorId { get; init; }

    [GreaterThanOrEqualTo(-50)]
    [LessThanOrEqualTo(150)]
    public required double Value { get; init; }
}
```

### 2. Register the adapter

```csharp
builder.Services.AddMessageValidation(options =>
{
    options.MapSource<TemperatureReading>("sensors/+/temperature");
});

builder.Services.AddMessageImmediateValidations();
```

That's it. The pipeline will call the source-generated `TemperatureReading.Validate(...)` static method for every incoming message — no reflection, no DI lookup per validation, AOT- and trim-friendly.

## Why use this adapter?

| Concern | DataAnnotations / FluentValidation | Immediate.Validations |
|---|---|---|
| Reflection at runtime | ✅ Yes | ❌ None |
| AOT / trim compatibility | ⚠️ Partial | ✅ Full |
| Errors caught at compile time | ❌ No | ✅ Yes (analyzers + source gen) |
| Automatic NRT null checks | ❌ No | ✅ Yes |

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) >= 2.0.0
- [Immediate.Validations](https://www.nuget.org/packages/Immediate.Validations) >= 3.3.0
