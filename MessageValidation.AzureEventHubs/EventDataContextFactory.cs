using Azure.Messaging.EventHubs;

namespace MessageValidation.AzureEventHubs;

/// <summary>
/// Builds a protocol-agnostic <see cref="MessageContext"/> from an Azure Event Hubs
/// <see cref="EventData"/>. Shared by the <see cref="EventHubConsumerClient"/> and
/// <see cref="EventProcessorClient"/> adapters.
/// </summary>
public static class EventDataContextFactory
{
    /// <summary>
    /// Creates a <see cref="MessageContext"/> from an <see cref="EventData"/>.
    /// The <see cref="MessageContext.Source"/> is set to <paramref name="eventHubName"/>
    /// (the natural stream identifier — equivalent to a Kafka topic).
    /// </summary>
    /// <param name="data">The received Event Hubs event.</param>
    /// <param name="eventHubName">The Event Hubs entity name (the stream the event was read from).</param>
    /// <param name="partitionId">The partition the event was read from.</param>
    /// <returns>A fully populated <see cref="MessageContext"/>.</returns>
    public static MessageContext CreateContext(
        EventData data,
        string eventHubName,
        string partitionId)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(eventHubName);

        var metadata = new Dictionary<string, object>
        {
            ["eventhubs.eventHubName"] = eventHubName,
            ["eventhubs.partitionId"] = partitionId ?? string.Empty,
            ["eventhubs.partitionKey"] = data.PartitionKey ?? string.Empty,
            ["eventhubs.sequenceNumber"] = data.SequenceNumber,
            ["eventhubs.offset"] = data.Offset,
            ["eventhubs.enqueuedTime"] = data.EnqueuedTime.UtcDateTime,
            ["eventhubs.messageId"] = data.MessageId ?? string.Empty,
            ["eventhubs.correlationId"] = data.CorrelationId ?? string.Empty,
            ["eventhubs.contentType"] = data.ContentType ?? string.Empty,
            ["eventhubs.properties"] = data.Properties
        };

        return new MessageContext
        {
            Source = eventHubName,
            RawPayload = data.EventBody?.ToArray() ?? [],
            Metadata = metadata
        };
    }
}
