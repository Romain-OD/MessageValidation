using Confluent.Kafka;
using NSubstitute;

namespace MessageValidation.Kafka.Tests;

public class KafkaConsumerExtensionsTests
{
    private static ConsumeResult<string, byte[]> MakeResult(
        string topic,
        byte[] value,
        string key = "k",
        int partition = 0,
        long offset = 0)
    {
        return new ConsumeResult<string, byte[]>
        {
            Topic = topic,
            Partition = new Partition(partition),
            Offset = new Offset(offset),
            Message = new Message<string, byte[]>
            {
                Key = key,
                Value = value,
                Timestamp = new Timestamp(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            }
        };
    }

    [Fact]
    public async Task StartConsuming_ProcessesMessageThroughPipeline()
    {
        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();
        var payload = """{"SensorId":"room1","Value":22.5}"""u8.ToArray();
        var result = MakeResult("sensors.temperature", payload);

        var callCount = 0;
        consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (++callCount == 1) return result;
                throw new OperationCanceledException();
            });

        await consumer.StartConsuming(pipeline);

        await pipeline.Received(1).ProcessAsync(
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartConsuming_SetsTopicAsSource()
    {
        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();
        var payload = """{"SensorId":"room1","Value":22.5}"""u8.ToArray();
        var result = MakeResult("sensors.temperature", payload);

        var callCount = 0;
        consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (++callCount == 1) return result;
                throw new OperationCanceledException();
            });

        MessageContext? captured = null;
        await pipeline.ProcessAsync(
            Arg.Do<MessageContext>(ctx => captured = ctx),
            Arg.Any<CancellationToken>());

        await consumer.StartConsuming(pipeline);

        Assert.NotNull(captured);
        Assert.Equal("sensors.temperature", captured.Source);
    }

    [Fact]
    public async Task StartConsuming_SetsRawPayload()
    {
        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();
        var payload = """{"SensorId":"room1","Value":22.5}"""u8.ToArray();
        var result = MakeResult("sensors.temperature", payload);

        var callCount = 0;
        consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (++callCount == 1) return result;
                throw new OperationCanceledException();
            });

        MessageContext? captured = null;
        await pipeline.ProcessAsync(
            Arg.Do<MessageContext>(ctx => captured = ctx),
            Arg.Any<CancellationToken>());

        await consumer.StartConsuming(pipeline);

        Assert.NotNull(captured);
        Assert.Equal(payload, captured.RawPayload);
    }

    [Fact]
    public async Task StartConsuming_PopulatesKafkaMetadata()
    {
        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();
        var result = MakeResult("my.topic", [1, 2, 3], key: "msg-key", partition: 2, offset: 99);

        var callCount = 0;
        consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (++callCount == 1) return result;
                throw new OperationCanceledException();
            });

        MessageContext? captured = null;
        await pipeline.ProcessAsync(
            Arg.Do<MessageContext>(ctx => captured = ctx),
            Arg.Any<CancellationToken>());

        await consumer.StartConsuming(pipeline);

        Assert.NotNull(captured);
        Assert.Equal("my.topic", captured.Metadata["kafka.topic"]);
        Assert.Equal(2, captured.Metadata["kafka.partition"]);
        Assert.Equal(99L, captured.Metadata["kafka.offset"]);
        Assert.Equal("msg-key", captured.Metadata["kafka.key"]);
        Assert.IsType<DateTime>(captured.Metadata["kafka.timestamp"]);
    }

    [Fact]
    public async Task StartConsuming_SkipsNullMessages()
    {
        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();

        var callCount = 0;
        consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (++callCount == 1)
                    return new ConsumeResult<string, byte[]> { Message = null };
                throw new OperationCanceledException();
            });

        await consumer.StartConsuming(pipeline);

        await pipeline.DidNotReceive().ProcessAsync(
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartConsuming_WithTopics_SubscribesBeforeConsuming()
    {
        var consumer = Substitute.For<IConsumer<string, byte[]>>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();

        consumer.Consume(Arg.Any<CancellationToken>())
            .Returns(_ => throw new OperationCanceledException());

        await consumer.StartConsuming(pipeline, topics: ["topic.a", "topic.b"]);

        consumer.Received(1).Subscribe(Arg.Is<IEnumerable<string>>(
            t => t.SequenceEqual(new[] { "topic.a", "topic.b" })));
    }
}
