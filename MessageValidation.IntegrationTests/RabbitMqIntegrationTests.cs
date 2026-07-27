using MessageValidation.RabbitMQ;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace MessageValidation.IntegrationTests;

public sealed class RabbitMqIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublishedMessage_ReachesValidationPipeline()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await using var container = new RabbitMqBuilder("rabbitmq:4.1.8").Build();
        await container.StartAsync(timeout.Token);

        var connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(container.GetConnectionString())
        };

        await using var connection = await connectionFactory.CreateConnectionAsync(timeout.Token);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: timeout.Token);

        var queue = $"message-validation-{Guid.NewGuid():N}";
        await channel.QueueDeclareAsync(
            queue,
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: timeout.Token);

        var pipeline = new RecordingPipeline();
        await channel.UseMessageValidation(pipeline, queue, ct: timeout.Token);

        var payload = """{"transport":"rabbitmq"}"""u8.ToArray();
        await channel.BasicPublishAsync(string.Empty, queue, payload, timeout.Token);

        var context = await pipeline.WaitAsync(timeout.Token);

        Assert.Equal(queue, context.Source);
        Assert.Equal(payload, context.RawPayload);
        Assert.Equal(queue, context.Metadata["rabbitmq.routingKey"]);
    }
}
