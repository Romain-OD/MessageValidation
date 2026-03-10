# MessageValidation.DataAnnotations

A **DataAnnotations adapter** for [MessageValidation](https://github.com/Romain-OD/MessageValidation) - validate messages using standard `System.ComponentModel.DataAnnotations` attributes in the MessageValidation pipeline.

## Installation

```bash
dotnet add package MessageValidation.DataAnnotations
```

## Quick Start

### 1. Decorate your message with attributes

```csharp
using System.ComponentModel.DataAnnotations;

public class TemperatureReading
{
    [Required(ErrorMessage = "SensorId is required.")]
    public string SensorId { get; set; } = "";

    [Range(-50, 150, ErrorMessage = "Value must be between -50 and 150.")]
    public double Value { get; set; }
}
```

### 2. Register the adapter

```csharp
builder.Services.AddMessageValidation(options =>
{
    options.MapSource<TemperatureReading>("sensors/+/temperature");
});

builder.Services.AddMessageDataAnnotationsValidation();
```

That's it. The pipeline will automatically validate any message using its DataAnnotations attributes.

## Supported attributes

All standard `System.ComponentModel.DataAnnotations` attributes are supported, including `[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]`, `[EmailAddress]`, `[Phone]`, and `[CustomValidation]`.

Messages implementing `IValidatableObject` are also supported for cross-property validation.

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) >= 0.2.0
