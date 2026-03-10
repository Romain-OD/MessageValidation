namespace MessageValidation.DataAnnotations.Tests;

public class DataAnnotationsMessageValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidMessage_ReturnsSuccess()
    {
        var adapter = new DataAnnotationsMessageValidator<TestMessage>();

        var result = await adapter.ValidateAsync(new TestMessage { Name = "hello", Value = 42 });

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_InvalidMessage_ReturnsErrors()
    {
        var adapter = new DataAnnotationsMessageValidator<TestMessage>();

        var result = await adapter.ValidateAsync(new TestMessage { Name = "", Value = 0 });

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Value");
    }

    [Fact]
    public async Task ValidateAsync_PartiallyInvalid_ReturnsOnlyFailedErrors()
    {
        var adapter = new DataAnnotationsMessageValidator<TestMessage>();

        var result = await adapter.ValidateAsync(new TestMessage { Name = "valid", Value = 0 });

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Value", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task ValidateAsync_ErrorMessages_AreMapped()
    {
        var adapter = new DataAnnotationsMessageValidator<TestMessage>();

        var result = await adapter.ValidateAsync(new TestMessage { Name = "", Value = 0 });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Name is required.");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Value must be between 1 and 100.");
    }

    [Fact]
    public async Task ValidateAsync_NoAttributes_ReturnsSuccess()
    {
        var adapter = new DataAnnotationsMessageValidator<NoAttributesMessage>();

        var result = await adapter.ValidateAsync(new NoAttributesMessage());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_IValidatableObject_ValidatesCustomRules()
    {
        var adapter = new DataAnnotationsMessageValidator<ValidatableMessage>();

        var result = await adapter.ValidateAsync(new ValidatableMessage
        {
            Start = new DateTime(2025, 6, 15),
            End = new DateTime(2025, 6, 10)
        });

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("End", result.Errors[0].PropertyName);
        Assert.Contains("End must be after Start", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_IValidatableObject_ValidDates_ReturnsSuccess()
    {
        var adapter = new DataAnnotationsMessageValidator<ValidatableMessage>();

        var result = await adapter.ValidateAsync(new ValidatableMessage
        {
            Start = new DateTime(2025, 6, 10),
            End = new DateTime(2025, 6, 15)
        });

        Assert.True(result.IsValid);
    }
}
