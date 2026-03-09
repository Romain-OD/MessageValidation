namespace MessageValidation.Tests;

public class MessageValidationOptionsTests
{
    [Fact]
    public void MapSource_ExactMatch_ResolvesCorrectType()
    {
        var options = new MessageValidationOptions();
        options.MapSource<TestMessage>("sensors/temperature");

        var found = options.TryResolveMessageType("sensors/temperature", out var type);

        Assert.True(found);
        Assert.Equal(typeof(TestMessage), type);
    }

    [Fact]
    public void TryResolveMessageType_NoMapping_ReturnsFalse()
    {
        var options = new MessageValidationOptions();

        var found = options.TryResolveMessageType("unknown/topic", out var type);

        Assert.False(found);
        Assert.Null(type);
    }

    [Fact]
    public void MapSource_SingleLevelWildcard_MatchesSingleSegment()
    {
        var options = new MessageValidationOptions();
        options.MapSource<TestMessage>("sensors/+/temperature");

        var found = options.TryResolveMessageType("sensors/kitchen/temperature", out var type);

        Assert.True(found);
        Assert.Equal(typeof(TestMessage), type);
    }

    [Fact]
    public void MapSource_SingleLevelWildcard_DoesNotMatchMultipleSegments()
    {
        var options = new MessageValidationOptions();
        options.MapSource<TestMessage>("sensors/+/temperature");

        var found = options.TryResolveMessageType("sensors/floor1/kitchen/temperature", out _);

        Assert.False(found);
    }

    [Fact]
    public void MapSource_MultiLevelWildcard_MatchesAllDescendants()
    {
        var options = new MessageValidationOptions();
        options.MapSource<TestMessage>("devices/#");

        Assert.True(options.TryResolveMessageType("devices/abc", out _));
        Assert.True(options.TryResolveMessageType("devices/abc/status", out _));
        Assert.True(options.TryResolveMessageType("devices/abc/status/battery", out _));
    }

    [Fact]
    public void MapSource_MultiLevelWildcard_DoesNotMatchDifferentPrefix()
    {
        var options = new MessageValidationOptions();
        options.MapSource<TestMessage>("devices/#");

        var found = options.TryResolveMessageType("sensors/abc", out _);

        Assert.False(found);
    }

    [Fact]
    public void MapSource_ExactMatchTakesPrecedenceOverWildcard()
    {
        var options = new MessageValidationOptions();
        options.MapSource<TestMessage>("sensors/+/temperature");

        // Exact source that doesn't match the pattern
        var found = options.TryResolveMessageType("sensors/temperature", out _);

        Assert.False(found);
    }

    [Fact]
    public void DefaultFailureBehavior_IsLog()
    {
        var options = new MessageValidationOptions();

        Assert.Equal(FailureBehavior.Log, options.DefaultFailureBehavior);
    }

    [Fact]
    public void DefaultDeadLetterPrefix_IsSet()
    {
        var options = new MessageValidationOptions();

        Assert.Equal("$dead-letter/", options.DeadLetterPrefix);
    }
}
