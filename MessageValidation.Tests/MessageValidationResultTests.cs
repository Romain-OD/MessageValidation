namespace MessageValidation.Tests;

public class MessageValidationResultTests
{
    [Fact]
    public void Success_IsValid()
    {
        var result = MessageValidationResult.Success();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_IsNotValid()
    {
        var errors = new[] { new MessageValidationError("Prop", "Error") };

        var result = MessageValidationResult.Failure(errors);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Prop", result.Errors[0].PropertyName);
        Assert.Equal("Error", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Failure_MultipleErrors_PreservesAll()
    {
        var errors = new[]
        {
            new MessageValidationError("A", "Error A"),
            new MessageValidationError("B", "Error B"),
            new MessageValidationError("C", "Error C")
        };

        var result = MessageValidationResult.Failure(errors);

        Assert.Equal(3, result.Errors.Count);
    }

    [Fact]
    public void Failure_EmptyErrors_IsValid()
    {
        var result = MessageValidationResult.Failure([]);

        Assert.True(result.IsValid);
    }
}
