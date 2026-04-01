using Confluent.Kafka;

namespace MessageValidation.Kafka;

/// <summary>
/// Extension methods for integrating the MessageValidation pipeline with a Confluent Kafka consumer.
/// </summary>
public static class KafkaConsumerExtensions
{
    /// <summary>
    /// Subscribes the consumer to the given <paramref name="topics"/> and starts a
    /// background consume loop that feeds every message into the
    /// <see cref="IMessageValidationPipeline"/> for deserialization, validation, and dispatch.
    /// </summary>
    /// <param name="consumer">The Confluent Kafka consumer (raw-byte value type).</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <param name="topics">One or more Kafka topic names to subscribe to.</param>
    /// <param name="ct">Cancellation token used to stop the consume loop.</param>
    /// <returns>
    /// A <see cref="Task"/> that completes when <paramref name="ct"/> is cancelled
    /// or the consumer is closed.
    /// </returns>
    public static Task StartConsuming(
        this IConsumer<string, byte[]> consumer,
        IMessageValidationPipeline pipeline,
        IEnumerable<string> topics,
        CancellationToken ct = default)
    {
        consumer.Subscribe(topics);
        return consumer.StartConsuming(pipeline, ct);
    }

    /// <summary>
    /// Starts a background consume loop that feeds every message already subscribed to
    /// into the <see cref="IMessageValidationPipeline"/> for deserialization, validation,
    /// and dispatch.
    /// </summary>
    /// <param name="consumer">The Confluent Kafka consumer (raw-byte value type).</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <param name="ct">Cancellation token used to stop the consume loop.</param>
    /// <returns>
    /// A <see cref="Task"/> that completes when <paramref name="ct"/> is cancelled
    /// or the consumer is closed.
    /// </returns>
    public static Task StartConsuming(
        this IConsumer<string, byte[]> consumer,
        IMessageValidationPipeline pipeline,
        CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]> result;
                try
                {
                    result = consumer.Consume(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (result?.Message is null)
                    continue;

                var context = new MessageContext
                {
                    Source = result.Topic,
                    RawPayload = result.Message.Value ?? [],
                    Metadata = new Dictionary<string, object>
                    {
                        ["kafka.topic"] = result.Topic,
                        ["kafka.partition"] = result.Partition.Value,
                        ["kafka.offset"] = result.Offset.Value,
                        ["kafka.key"] = result.Message.Key ?? string.Empty,
                        ["kafka.timestamp"] = result.Message.Timestamp.UtcDateTime
                    }
                };

                await pipeline.ProcessAsync(context);
            }
        }, CancellationToken.None);
    }
}
