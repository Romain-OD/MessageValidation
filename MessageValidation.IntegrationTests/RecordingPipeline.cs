namespace MessageValidation.IntegrationTests;

internal sealed class RecordingPipeline : IMessageValidationPipeline
{
    private readonly TaskCompletionSource<MessageContext> _received =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task ProcessAsync(MessageContext context, CancellationToken ct = default)
    {
        _received.TrySetResult(context);
        return Task.CompletedTask;
    }

    public Task<MessageContext> WaitAsync(CancellationToken ct) =>
        _received.Task.WaitAsync(ct);
}
