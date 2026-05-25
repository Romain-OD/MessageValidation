using Immediate.Validations.Shared;

namespace MessageValidation.ImmediateValidations.Tests;

[Validate]
public partial record TestMessage : IValidationTarget<TestMessage>
{
    public required string Name { get; init; }

    public required int Value { get; init; }

    private static void AdditionalValidations(
        ValidationResult errors,
        TestMessage target)
    {
        if (string.IsNullOrWhiteSpace(target.Name))
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(Name),
                ErrorMessage = "Name is required.",
            });
        }

        if (target.Value is < 1 or > 100)
        {
            errors.Add(new ValidationError
            {
                PropertyName = nameof(Value),
                ErrorMessage = "Value must be between 1 and 100.",
            });
        }
    }
}

[Validate]
public partial record NoRulesMessage : IValidationTarget<NoRulesMessage>
{
    public string Data { get; init; } = "";
}
