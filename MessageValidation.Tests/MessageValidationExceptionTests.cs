namespace MessageValidation.Tests;

public class MessageValidationExceptionTests
{
    [Fact]
    public void Constructor_SetsValidationResult()
    {
        var errors = new[] { new MessageValidationError("Name", "Required") };
        var result = MessageValidationResult.Failure(errors);

        var exception = new MessageValidationException(result);

        Assert.Same(result, exception.ValidationResult);
        Assert.Contains("Name", exception.Message);
        Assert.Contains("Required", exception.Message);
    }
}
