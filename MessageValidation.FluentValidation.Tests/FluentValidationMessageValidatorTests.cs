using FluentValidation;

namespace MessageValidation.FluentValidation.Tests;

public class FluentValidationMessageValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidMessage_ReturnsSuccess()
    {
        var fluentValidator = new TestMessageValidator();
        var adapter = new FluentValidationMessageValidator<TestMessage>(fluentValidator);

        var result = await adapter.ValidateAsync(new TestMessage { Name = "hello", Value = 42 });

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_InvalidMessage_ReturnsErrors()
    {
        var fluentValidator = new TestMessageValidator();
        var adapter = new FluentValidationMessageValidator<TestMessage>(fluentValidator);

        var result = await adapter.ValidateAsync(new TestMessage { Name = "", Value = -1 });

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Value");
    }

    [Fact]
    public async Task ValidateAsync_PartiallyInvalid_ReturnsOnlyFailedErrors()
    {
        var fluentValidator = new TestMessageValidator();
        var adapter = new FluentValidationMessageValidator<TestMessage>(fluentValidator);

        var result = await adapter.ValidateAsync(new TestMessage { Name = "valid", Value = -1 });

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Value", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task ValidateAsync_NoRules_ReturnsSuccess()
    {
        var fluentValidator = new AlwaysValidValidator();
        var adapter = new FluentValidationMessageValidator<NoRulesMessage>(fluentValidator);

        var result = await adapter.ValidateAsync(new NoRulesMessage());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_ErrorMessages_AreMapped()
    {
        var fluentValidator = new TestMessageValidator();
        var adapter = new FluentValidationMessageValidator<TestMessage>(fluentValidator);

        var result = await adapter.ValidateAsync(new TestMessage { Name = "", Value = 0 });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Name is required.");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Value must be positive.");
    }
}
