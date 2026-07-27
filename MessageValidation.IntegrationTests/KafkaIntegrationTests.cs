using Confluent.Kafka;
using MessageValidation.Kafka;
using Testcontainers.Kafka;

namespace MessageValidation.IntegrationTests;

public sealed class KafkaIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ProducedMessage_ReachesValidationPipeline()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await using var container = new KafkaBuilder("confluentinc/cp-kafka:7.5.12").Build();
        await container.StartAsync(timeout.Token);

        var topic = $"message-validation-{Guid.NewGuid():N}";
        var payload = """{"transport":"kafka"}"""u8.ToArray();

        using (var producer = new ProducerBuilder<string, byte[]>(new ProducerConfig
        {
            BootstrapServers = container.GetBootstrapAddress()
        }).Build())
        {
            await producer.ProduceAsync(
                topic,
                new Message<string, byte[]> { Key = "integration", Value = payload },
                timeout.Token);
        }

        using var consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = container.GetBootstrapAddress(),
            GroupId = $"message-validation-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

        using var consumeCancellation = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
        var pipeline = new RecordingPipeline();
        var consumeTask = consumer.StartConsuming(pipeline, [topic], consumeCancellation.Token);

        MessageContext context;
        try
        {
            context = await pipeline.WaitAsync(timeout.Token);
        }
        finally
        {
            await consumeCancellation.CancelAsync();
            await consumeTask;
            consumer.Close();
        }

        Assert.Equal(topic, context.Source);
        Assert.Equal(payload, context.RawPayload);
        Assert.Equal(topic, context.Metadata["kafka.topic"]);
    }
}
