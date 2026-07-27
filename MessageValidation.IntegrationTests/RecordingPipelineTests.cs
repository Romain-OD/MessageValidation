namespace MessageValidation.IntegrationTests;

public sealed class RecordingPipelineTests
{
    [Fact]
    public async Task Fail_CompletesWaitWithException()
    {
        var pipeline = new RecordingPipeline();
        var expected = new InvalidOperationException("processor failed");

        pipeline.Fail(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pipeline.WaitAsync(CancellationToken.None));

        Assert.Same(expected, actual);
    }
}
