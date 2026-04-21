using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

namespace MessageValidation.AzureEventHubs;

/// <summary>
/// Extension methods for integrating the MessageValidation pipeline with the
/// low-level <see cref="EventHubConsumerClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EventHubConsumerClient"/> is the lightweight, single-process consumer
/// from the <c>Azure.Messaging.EventHubs</c> SDK. It does NOT manage checkpoints or
/// partition ownership — use <c>EventProcessorClient</c> for production workloads
/// that need those features.
/// </para>
/// </remarks>
public static class EventHubConsumerClientExtensions
{
    /// <summary>
    /// Reads events from all partitions of the event hub and feeds each one through
    /// the <see cref="IMessageValidationPipeline"/> for deserialization, validation,
    /// and dispatch.
    /// </summary>
    /// <param name="consumer">The low-level Event Hubs consumer client.</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <param name="startReadingAtEarliestEvent">
    /// When <see langword="true"/>, start reading from the earliest event; otherwise
    /// read only newly enqueued events. Defaults to <see langword="false"/>.
    /// </param>
    /// <param name="ct">Cancellation token used to stop the read loop.</param>
    /// <returns>A <see cref="Task"/> that completes when the loop is cancelled.</returns>
    public static async Task StartConsuming(
        this EventHubConsumerClient consumer,
        IMessageValidationPipeline pipeline,
        bool startReadingAtEarliestEvent = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(pipeline);

        try
        {
            await foreach (var partitionEvent in consumer
                .ReadEventsAsync(startReadingAtEarliestEvent, cancellationToken: ct)
                .ConfigureAwait(false))
            {
                if (partitionEvent.Data is null)
                    continue;

                var context = EventDataContextFactory.CreateContext(
                    partitionEvent.Data,
                    consumer.EventHubName,
                    partitionEvent.Partition.PartitionId);

                await pipeline.ProcessAsync(context, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation — exit cleanly.
        }
    }
}
