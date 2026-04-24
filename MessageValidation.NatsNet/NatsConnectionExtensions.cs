using NATS.Client.Core;

namespace MessageValidation.NatsNet;

/// <summary>
/// Extension methods for integrating the MessageValidation pipeline with a NATS.Net connection.
/// </summary>
public static class NatsConnectionExtensions
{
    /// <summary>
    /// Subscribes the NATS connection to the specified subject and pushes every incoming
    /// message through the <see cref="IMessageValidationPipeline"/> so it is automatically
    /// deserialized, validated, and dispatched.
    /// </summary>
    /// <param name="connection">The NATS connection.</param>
    /// <param name="subject">The subject to subscribe to (supports <c>*</c> and <c>&gt;</c> wildcards).</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <param name="queueGroup">
    /// Optional queue group name. When set, messages are load-balanced across all subscribers
    /// sharing the same queue group.
    /// </param>
    /// <param name="ct">A token to cancel the subscription loop.</param>
    /// <returns>A task that completes when the subscription loop is cancelled.</returns>
    public static async Task SubscribeWithMessageValidationAsync(
        this INatsConnection connection,
        string subject,
        IMessageValidationPipeline pipeline,
        string? queueGroup = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrEmpty(subject);
        ArgumentNullException.ThrowIfNull(pipeline);

        await foreach (var msg in connection.SubscribeAsync<byte[]>(
            subject,
            queueGroup: queueGroup,
            cancellationToken: ct).ConfigureAwait(false))
        {
            var context = new MessageContext
            {
                Source = msg.Subject,
                RawPayload = msg.Data ?? Array.Empty<byte>(),
                Metadata = new Dictionary<string, object>
                {
                    ["nats.subject"] = msg.Subject,
                    ["nats.replyTo"] = msg.ReplyTo ?? string.Empty,
                    ["nats.headers"] = (object?)msg.Headers ?? string.Empty,
                    ["nats.queueGroup"] = queueGroup ?? string.Empty
                }
            };

            await pipeline.ProcessAsync(context, ct).ConfigureAwait(false);
        }
    }
}
