namespace MessageValidation.ImmediateValidations.Tests;

public class ImmediateValidationsMessageValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidMessage_ReturnsSuccess()
    {
        var adapter = new ImmediateValidationsMessageValidator<TestMessage>();

        var result = await adapter.ValidateAsync(new TestMessage { Name = "hello", Value = 42 });

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_InvalidMessage_ReturnsErrors()
    {
        var adapter = new ImmediateValidationsMessageValidator<TestMessage>();

        var result = await adapter.ValidateAsync(new TestMessage { Name = "", Value = 0 });

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Value");
    }

    [Fact]
    public async Task ValidateAsync_PartiallyInvalid_ReturnsOnlyFailedErrors()
    {
        var adapter = new ImmediateValidationsMessageValidator<TestMessage>();

        var result = await adapter.ValidateAsync(new TestMessage { Name = "valid", Value = 0 });

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Value", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task ValidateAsync_ErrorMessages_AreMapped()
    {
        var adapter = new ImmediateValidationsMessageValidator<TestMessage>();

        var result = await adapter.ValidateAsync(new TestMessage { Name = "", Value = 0 });

        Assert.Contains(result.Errors, e => e.ErrorMessage == "Name is required.");
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Value must be between 1 and 100.");
    }

    [Fact]
    public async Task ValidateAsync_NoRules_ReturnsSuccess()
    {
        var adapter = new ImmediateValidationsMessageValidator<NoRulesMessage>();

        var result = await adapter.ValidateAsync(new NoRulesMessage());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_RespectsCancellationToken_Parameter()
    {
        // The adapter is purely synchronous source-gen logic, so the token is
        // not observed — but it should still accept one and complete successfully.
        var adapter = new ImmediateValidationsMessageValidator<TestMessage>();

        using var cts = new CancellationTokenSource();
        var result = await adapter.ValidateAsync(
            new TestMessage { Name = "ok", Value = 10 },
            cts.Token);

        Assert.True(result.IsValid);
    }
}
