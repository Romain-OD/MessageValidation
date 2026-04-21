# MessageValidation.AzureServiceBus

[Azure Service Bus](https://learn.microsoft.com/azure/service-bus-messaging/) transport adapter for the [MessageValidation](../MessageValidation/README.md) pipeline — automatically feed incoming Service Bus messages into the validation pipeline with a single line of code.

Works with the official [`Azure.Messaging.ServiceBus`](https://www.nuget.org/packages/Azure.Messaging.ServiceBus) SDK. Supports both `ServiceBusProcessor` (queues and subscriptions) and `ServiceBusSessionProcessor` (FIFO / session-enabled entities).

## Installation

```bash
dotnet add package MessageValidation.AzureServiceBus
```

## Quick Start

### Option A — Extension method on `ServiceBusProcessor`

Wire the pipeline directly onto an existing processor:

```csharp
using Azure.Messaging.ServiceBus;
using MessageValidation.AzureServiceBus;

var client = new ServiceBusClient("<connection-string>");
var processor = client.CreateProcessor("orders", new ServiceBusProcessorOptions
{
    AutoCompleteMessages = true,
    MaxConcurrentCalls = 4
});

var pipeline = serviceProvider.GetRequiredService<IMessageValidationPipeline>();

// All messages go through the validation pipeline
processor.UseMessageValidation(pipeline);

await processor.StartProcessingAsync();
```

### Option B — DI registration

Let the DI container create the `ServiceBusClient`:

```csharp
using MessageValidation;
using MessageValidation.AzureServiceBus;

builder.Services.AddMessageValidation(options =>
{
    options.MapSource<OrderCreated>("order.created");
    options.MapSource<OrderCancelled>("order.cancelled");
    options.DefaultFailureBehavior = FailureBehavior.DeadLetter;
});

builder.Services.AddMessageDeserializer<JsonMessageDeserializer>();
builder.Services.AddMessageHandler<OrderCreated, OrderCreatedHandler>();
builder.Services.AddMessageHandler<OrderCancelled, OrderCancelledHandler>();

// Registers a singleton ServiceBusClient
builder.Services.AddAzureServiceBusMessageValidation(
    builder.Configuration.GetConnectionString("ServiceBus")!);
```

Then inject `ServiceBusClient` and start the processor from a `BackgroundService`:

```csharp
public class OrdersWorker(
    ServiceBusClient client,
    IMessageValidationPipeline pipeline) : BackgroundService
{
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _processor = client.CreateProcessor("orders");
        _processor.UseMessageValidation(pipeline);
        await _processor.StartProcessingAsync(ct);
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(ct);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(ct);
    }
}
```

### Option C — Passwordless auth (`TokenCredential`)

```csharp
using Azure.Identity;

builder.Services.AddAzureServiceBusMessageValidation(
    fullyQualifiedNamespace: "contoso.servicebus.windows.net",
    credential: new DefaultAzureCredential());
```

### Session-enabled entities

```csharp
var sessionProcessor = client.CreateSessionProcessor("orders-fifo");
sessionProcessor.UseMessageValidation(pipeline);
await sessionProcessor.StartProcessingAsync();
```

## Source resolution

The adapter resolves `MessageContext.Source` using the following rule:

1. If `ServiceBusReceivedMessage.Subject` (a.k.a. `Label`) is set → use it.
2. Otherwise → fall back to the processor's `EntityPath` (queue / topic / subscription name).

This lets you fan-in multiple message types onto a single queue and route each one to the right type via `MapSource<T>("subject-name")`.

## Service Bus metadata

When the adapter creates a `MessageContext`, it populates the `Metadata` dictionary with Service Bus properties:

| Key | Type | Description |
|---|---|---|
| `servicebus.entityPath` | `string` | Queue / topic / subscription path |
| `servicebus.messageId` | `string` | Message ID |
| `servicebus.subject` | `string` | Subject (Label) |
| `servicebus.correlationId` | `string` | Correlation ID |
| `servicebus.contentType` | `string` | Content type |
| `servicebus.sessionId` | `string` | Session ID (when applicable) |
| `servicebus.enqueuedTime` | `DateTime` | Enqueued timestamp (UTC) |
| `servicebus.deliveryCount` | `int` | Number of delivery attempts |
| `servicebus.applicationProperties` | `IReadOnlyDictionary<string, object>` | Custom application properties |

Access them in your handler:

```csharp
public class OrderCreatedHandler : IMessageHandler<OrderCreated>
{
    public Task HandleAsync(
        OrderCreated message, MessageContext context, CancellationToken ct = default)
    {
        var deliveryCount = (int)context.Metadata["servicebus.deliveryCount"];
        var correlationId = (string)context.Metadata["servicebus.correlationId"];
        Console.WriteLine($"[corr={correlationId} attempt={deliveryCount}] Order {message.OrderId}");
        return Task.CompletedTask;
    }
}
```

## Auto-complete vs. manual settlement

By default, `ServiceBusProcessor` has `AutoCompleteMessages = true`:

- Pipeline returns normally → message is **completed**.
- Pipeline throws (e.g., `FailureBehavior.ThrowException`) → message is **abandoned** and retried up to `MaxDeliveryCount`.
- Message exceeds `MaxDeliveryCount` → Service Bus moves it to the entity's built-in **dead-letter subqueue**.

If you prefer explicit settlement, set `AutoCompleteMessages = false` and perform settlement from a custom `IValidationFailureHandler` using the `servicebus.*` metadata — or combine the native Service Bus dead-letter with `FailureBehavior.Log` / `FailureBehavior.Skip`.

## API Reference

### `ServiceBusProcessor.UseMessageValidation(pipeline, onError?)`

Hooks `ProcessMessageAsync` so every received message flows through the pipeline. Registers a no-op `ProcessErrorAsync` handler (replace it via the `onError` parameter).

### `ServiceBusSessionProcessor.UseMessageValidation(pipeline, onError?)`

Same as above, for session-enabled entities.

### `ServiceBusProcessorExtensions.CreateContext(message, entityPath)`

Helper that builds a `MessageContext` from a `ServiceBusReceivedMessage`. Useful when you want to reuse the pipeline from a custom processor or a `ServiceBusReceiver`.

### `AddAzureServiceBusMessageValidation(connectionString, configureOptions?)`

Registers a singleton `ServiceBusClient` built from a connection string.

### `AddAzureServiceBusMessageValidation(fullyQualifiedNamespace, credential, configureOptions?)`

Registers a singleton `ServiceBusClient` built from a namespace and a `TokenCredential` (passwordless auth).

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) core package
- [Azure.Messaging.ServiceBus](https://www.nuget.org/packages/Azure.Messaging.ServiceBus) 7+

## License

[MIT](../LICENSE)
