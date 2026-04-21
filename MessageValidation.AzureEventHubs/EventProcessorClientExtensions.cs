using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;

namespace MessageValidation.AzureEventHubs;

/// <summary>
/// Extension methods for integrating the MessageValidation pipeline with the
/// high-level <see cref="EventProcessorClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EventProcessorClient"/> is the recommended, production-grade consumer
/// for Azure Event Hubs. It handles partition ownership and checkpointing via an
/// Azure Blob Storage container.
/// </para>
/// <para>
/// The adapter does NOT call <see cref="ProcessEventArgs.UpdateCheckpointAsync(CancellationToken)"/>
/// for you — checkpointing frequency is a business decision and should be made by
/// the caller (e.g., every N events, on a timer, or from a custom
/// <see cref="IMessageHandler{TMessage}"/> using the <c>eventhubs.*</c> metadata).
/// </para>
/// </remarks>
public static class EventProcessorClientExtensions
{
    /// <summary>
    /// Hooks the <see cref="IMessageValidationPipeline"/> into the processor's
    /// <see cref="EventProcessorClient.ProcessEventAsync"/> event so that every
    /// received event is automatically deserialized, validated, and dispatched.
    /// Also wires a minimal <see cref="EventProcessorClient.ProcessErrorAsync"/>
    /// handler (required by the SDK).
    /// </summary>
    /// <param name="processor">The Event Hubs processor client.</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <param name="onError">
    /// Optional error callback. When omitted, errors are swallowed silently.
    /// </param>
    /// <returns>The same <see cref="EventProcessorClient"/> for chaining.</returns>
    public static EventProcessorClient UseMessageValidation(
        this EventProcessorClient processor,
        IMessageValidationPipeline pipeline,
        Func<ProcessErrorEventArgs, Task>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(pipeline);

        processor.ProcessEventAsync += async args =>
        {
            if (!args.HasEvent || args.Data is null)
                return;

            var context = EventDataContextFactory.CreateContext(
                args.Data,
                args.Partition.EventHubName,
                args.Partition.PartitionId);

            await pipeline.ProcessAsync(context, args.CancellationToken).ConfigureAwait(false);
        };

        processor.ProcessErrorAsync += onError ?? (_ => Task.CompletedTask);

        return processor;
    }
}
