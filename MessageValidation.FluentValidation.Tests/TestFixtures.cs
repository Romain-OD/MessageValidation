using FluentValidation;

namespace MessageValidation.FluentValidation.Tests;

public class TestMessage
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

public class TestMessageValidator : AbstractValidator<TestMessage>
{
    public TestMessageValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Value).GreaterThan(0).WithMessage("Value must be positive.");
    }
}

public class AlwaysValidValidator : AbstractValidator<TestMessage>
{
    // No rules — always passes
}
