# MessageValidation.AzureEventHubs

[Azure Event Hubs](https://learn.microsoft.com/azure/event-hubs/) transport adapter for the [MessageValidation](../MessageValidation/README.md) pipeline — automatically feed incoming Event Hubs events into the validation pipeline with a single line of code.

Works with the official [`Azure.Messaging.EventHubs`](https://www.nuget.org/packages/Azure.Messaging.EventHubs) SDK. Supports both the high-level `EventProcessorClient` (production-grade — partition ownership + checkpointing) and the low-level `EventHubConsumerClient` (single-process, no checkpoints — great for dev/test).

## Installation

```bash
dotnet add package MessageValidation.AzureEventHubs
```

## Quick Start

### Option A — `EventProcessorClient` (recommended for production)

```csharp
using Azure.Messaging.EventHubs;
using Azure.Storage.Blobs;
using MessageValidation.AzureEventHubs;

var storage = new BlobContainerClient("<storage-conn>", "checkpoints");
var processor = new EventProcessorClient(
    storage,
    consumerGroup: EventHubConsumerClient.DefaultConsumerGroupName,
    connectionString: "<eventhubs-conn>",
    eventHubName: "telemetry");

var pipeline = serviceProvider.GetRequiredService<IMessageValidationPipeline>();

// All events flow through the validation pipeline
processor.UseMessageValidation(pipeline);

await processor.StartProcessingAsync();
```

> Checkpointing is intentionally **not** done by the adapter — call
> `args.UpdateCheckpointAsync(...)` from your own handler (e.g., every N events
> or on a timer) using the `eventhubs.*` metadata exposed on `MessageContext`.

### Option B — `EventHubConsumerClient` (single-process, no checkpoints)

Useful for dev, tests, one-shot scripts, or workloads that don't need partition ownership:

```csharp
using Azure.Messaging.EventHubs.Consumer;
using MessageValidation.AzureEventHubs;

var consumer = new EventHubConsumerClient(
    EventHubConsumerClient.DefaultConsumerGroupName,
    "<eventhubs-conn>",
    "telemetry");

var pipeline = serviceProvider.GetRequiredService<IMessageValidationPipeline>();

// Reads from all partitions and feeds every event through the pipeline
await consumer.StartConsuming(pipeline, startReadingAtEarliestEvent: false, ct);
```

### Option C — DI registration

Register an `EventHubConsumerClient` in the container:

```csharp
using MessageValidation;
using MessageValidation.AzureEventHubs;

builder.Services.AddMessageValidation(options =>
{
    options.MapSource<TelemetryReading>("telemetry");
    options.DefaultFailureBehavior = FailureBehavior.Log;
});

builder.Services.AddMessageDeserializer<JsonMessageDeserializer>();
builder.Services.AddMessageHandler<TelemetryReading, TelemetryHandler>();

builder.Services.AddAzureEventHubsMessageValidation(
    connectionString: builder.Configuration.GetConnectionString("EventHubs")!,
    eventHubName: "telemetry");
```

Then inject and consume from a `BackgroundService`:

```csharp
public class EventHubsWorker(
    EventHubConsumerClient consumer,
    IMessageValidationPipeline pipeline) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct) =>
        consumer.StartConsuming(pipeline, ct: ct);
}
```

### Passwordless auth (`TokenCredential`)

```csharp
using Azure.Identity;

builder.Services.AddAzureEventHubsMessageValidation(
    fullyQualifiedNamespace: "contoso.servicebus.windows.net",
    eventHubName: "telemetry",
    credential: new DefaultAzureCredential());
```

## Source resolution

Event Hubs doesn't have a per-event "subject" or "label" concept — an event hub IS the stream (comparable to a Kafka topic). The adapter therefore sets:

- `MessageContext.Source` = the **event hub name** (e.g., `"telemetry"`).

Map it to a CLR type with `MapSource<T>("telemetry")`. If you route multiple types on the same event hub, use `EventData.Properties["MessageType"]` in a custom handler or build your own `MessageContext` by calling `EventDataContextFactory.CreateContext(...)` and overriding `Source`.

## Event Hubs metadata

| Key | Type | Description |
|---|---|---|
| `eventhubs.eventHubName` | `string` | Event hub (entity) name |
| `eventhubs.partitionId` | `string` | Partition the event was read from |
| `eventhubs.partitionKey` | `string` | Partition key set by the producer |
| `eventhubs.sequenceNumber` | `long` | Monotonically increasing sequence number |
| `eventhubs.offset` | `long` | Offset within the partition |
| `eventhubs.enqueuedTime` | `DateTime` | Enqueued timestamp (UTC) |
| `eventhubs.messageId` | `string` | Message ID (if set) |
| `eventhubs.correlationId` | `string` | Correlation ID (if set) |
| `eventhubs.contentType` | `string` | Content type (if set) |
| `eventhubs.properties` | `IDictionary<string, object>` | Custom application properties |

```csharp
public class TelemetryHandler : IMessageHandler<TelemetryReading>
{
    public Task HandleAsync(
        TelemetryReading message, MessageContext context, CancellationToken ct = default)
    {
        var partition = (string)context.Metadata["eventhubs.partitionId"];
        var seq = (long)context.Metadata["eventhubs.sequenceNumber"];
        Console.WriteLine($"[p={partition} seq={seq}] {message.SensorId}: {message.Value}");
        return Task.CompletedTask;
    }
}
```

## API Reference

### `EventProcessorClient.UseMessageValidation(pipeline, onError?)`

Hooks `ProcessEventAsync` so every received event flows through the pipeline.
Registers a no-op `ProcessErrorAsync` handler (replace it via `onError`).

> The adapter does **not** call `UpdateCheckpointAsync` — do it yourself from
> your handler based on your delivery guarantees.

### `EventHubConsumerClient.StartConsuming(pipeline, startReadingAtEarliestEvent?, ct?)`

Starts an `await foreach` read loop over every partition and feeds each event through the pipeline. Exits cleanly when the cancellation token is cancelled. No checkpointing.

### `EventDataContextFactory.CreateContext(data, eventHubName, partitionId)`

Pure helper that builds a `MessageContext` from an `EventData`. Useful for custom consumers or tests.

### `AddAzureEventHubsMessageValidation(connectionString, eventHubName, consumerGroup?)`

Registers a singleton `EventHubConsumerClient` built from a connection string.

### `AddAzureEventHubsMessageValidation(fullyQualifiedNamespace, eventHubName, TokenCredential, consumerGroup?)`

Same, but using passwordless auth.

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) core package
- [Azure.Messaging.EventHubs](https://www.nuget.org/packages/Azure.Messaging.EventHubs) 5+
- [Azure.Messaging.EventHubs.Processor](https://www.nuget.org/packages/Azure.Messaging.EventHubs.Processor) 5+ (for `EventProcessorClient`)

## License

[MIT](../LICENSE)
