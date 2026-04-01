using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageValidation.RabbitMQ;

/// <summary>
/// Extension methods for integrating the MessageValidation pipeline with a RabbitMQ channel.
/// </summary>
public static class RabbitMqChannelExtensions
{
    /// <summary>
    /// Creates an <see cref="AsyncEventingBasicConsumer"/> wired to the
    /// <see cref="IMessageValidationPipeline"/> so that every delivered message
    /// is automatically deserialized, validated, and dispatched.
    /// </summary>
    /// <param name="channel">The RabbitMQ channel.</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <param name="queue">The queue to consume from.</param>
    /// <param name="autoAck">Whether to auto-acknowledge messages. Defaults to <c>true</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The consumer tag returned by RabbitMQ.</returns>
    public static async Task<string> UseMessageValidation(
        this IChannel channel,
        IMessageValidationPipeline pipeline,
        string queue,
        bool autoAck = true,
        CancellationToken ct = default)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var source = string.IsNullOrEmpty(ea.RoutingKey) ? queue : ea.RoutingKey;

            var metadata = new Dictionary<string, object>
            {
                ["rabbitmq.exchange"] = ea.Exchange,
                ["rabbitmq.routingKey"] = ea.RoutingKey,
                ["rabbitmq.deliveryTag"] = ea.DeliveryTag,
                ["rabbitmq.redelivered"] = ea.Redelivered,
                ["rabbitmq.consumerTag"] = ea.ConsumerTag
            };

            if (ea.BasicProperties is not null)
            {
                if (ea.BasicProperties.ContentType is not null)
                    metadata["rabbitmq.contentType"] = ea.BasicProperties.ContentType;

                if (ea.BasicProperties.CorrelationId is not null)
                    metadata["rabbitmq.correlationId"] = ea.BasicProperties.CorrelationId;

                if (ea.BasicProperties.MessageId is not null)
                    metadata["rabbitmq.messageId"] = ea.BasicProperties.MessageId;

                if (ea.BasicProperties.Headers is not null)
                    metadata["rabbitmq.headers"] = ea.BasicProperties.Headers;
            }

            var context = new MessageContext
            {
                Source = source,
                RawPayload = ea.Body.ToArray(),
                Metadata = metadata
            };

            await pipeline.ProcessAsync(context);
        };

        return await channel.BasicConsumeAsync(queue, autoAck, consumer, ct);
    }
}
