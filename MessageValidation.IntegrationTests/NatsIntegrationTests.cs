using MessageValidation.NatsNet;
using NATS.Client.Core;
using Testcontainers.Nats;

namespace MessageValidation.IntegrationTests;

public sealed class NatsIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublishedMessage_ReachesValidationPipeline()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await using var container = new NatsBuilder("nats:2.12.11").Build();
        await container.StartAsync(timeout.Token);

        await using var connection = new NatsConnection(
            NatsOpts.Default with { Url = container.GetConnectionString() });
        await connection.ConnectAsync();

        var subject = $"message-validation.{Guid.NewGuid():N}";
        var payload = """{"transport":"nats"}"""u8.ToArray();
        var pipeline = new RecordingPipeline();

        using var subscriptionCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        var subscriptionTask = connection.SubscribeWithMessageValidationAsync(
            subject,
            pipeline,
            ct: subscriptionCancellation.Token);

        MessageContext context;
        try
        {
            await connection.PingAsync(timeout.Token);
            await connection.PublishAsync(subject, payload, cancellationToken: timeout.Token);
            await connection.PingAsync(timeout.Token);
            context = await pipeline.WaitAsync(timeout.Token);
        }
        finally
        {
            await subscriptionCancellation.CancelAsync();
            try
            {
                await subscriptionTask;
            }
            catch (OperationCanceledException) when (subscriptionCancellation.IsCancellationRequested)
            {
            }
        }

        Assert.Equal(subject, context.Source);
        Assert.Equal(payload, context.RawPayload);
        Assert.Equal(subject, context.Metadata["nats.subject"]);
    }
}
