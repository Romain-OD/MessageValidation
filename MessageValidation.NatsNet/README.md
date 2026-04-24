# MessageValidation.NatsNet

[NATS.Net](https://github.com/nats-io/nats.net) transport adapter for the [MessageValidation](../MessageValidation/README.md) pipeline — automatically feed incoming NATS messages into the validation pipeline with a single line of code.

## Installation

```bash
dotnet add package MessageValidation.NatsNet
```

## Quick Start

### Option A — Extension method on `INatsConnection`

Wire the pipeline directly onto an existing NATS connection:

```csharp
using MessageValidation.NatsNet;
using NATS.Net;

await using var client = new NatsClient("nats://localhost:4222");
var connection = client.Connection;

var pipeline = serviceProvider.GetRequiredService<IMessageValidationPipeline>();

// One line — all incoming messages now go through the validation pipeline
await connection.SubscribeWithMessageValidationAsync(
    subject: "sensors.*.temperature",
    pipeline: pipeline,
    ct: stoppingToken);
```

### Option B — DI registration

Let the DI container create and configure the connection automatically:

```csharp
using MessageValidation;
using MessageValidation.NatsNet;

builder.Services.AddMessageValidation(options =>
{
    options.MapSource<TemperatureReading>("sensors.*.temperature");
    options.DefaultFailureBehavior = FailureBehavior.Log;
});

builder.Services.AddMessageDeserializer<JsonMessageDeserializer>();
builder.Services.AddMessageHandler<TemperatureReading, TemperatureHandler>();

// Register an INatsConnection ready to be used with the pipeline
builder.Services.AddNatsNetMessageValidation("nats://localhost:4222");
```

Then inject `INatsConnection` wherever you need it:

```csharp
public class NatsWorker(
    INatsConnection connection,
    IMessageValidationPipeline pipeline) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct) =>
        connection.SubscribeWithMessageValidationAsync(
            subject: "sensors.*.temperature",
            pipeline: pipeline,
            ct: ct);
}
```

### Queue groups (load balancing)

Use a queue group to load-balance messages across multiple subscribers:

```csharp
await connection.SubscribeWithMessageValidationAsync(
    subject: "orders.>",
    pipeline: pipeline,
    queueGroup: "order-workers",
    ct: stoppingToken);
```

## NATS Metadata

When the adapter creates a `MessageContext`, it populates the `Metadata` dictionary with NATS-specific properties:

| Key | Type | Description |
|---|---|---|
| `nats.subject` | `string` | The subject the message was delivered on |
| `nats.replyTo` | `string` | The reply-to subject, if any |
| `nats.headers` | `NatsHeaders` | Message headers, if any |
| `nats.queueGroup` | `string` | Queue group name, if subscribed with one |

Access them in your handler:

```csharp
public class TemperatureHandler : IMessageHandler<TemperatureReading>
{
    public Task HandleAsync(
        TemperatureReading message, MessageContext context, CancellationToken ct = default)
    {
        var subject = context.Metadata["nats.subject"];
        Console.WriteLine($"[{subject}] Sensor {message.SensorId}: {message.Value}°C");
        return Task.CompletedTask;
    }
}
```

## API Reference

### `INatsConnection.SubscribeWithMessageValidationAsync(string subject, IMessageValidationPipeline pipeline, string? queueGroup = null, CancellationToken ct = default)`

Subscribes to the specified subject and pushes every received message through the pipeline.
Returns a `Task` that completes when the subscription loop is cancelled.

### `AddNatsNetMessageValidation(string? url = null, Action<NatsOpts>? configureOptions = null)`

Registers a singleton `INatsConnection` in the DI container.

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) core package
- [NATS.Net](https://www.nuget.org/packages/NATS.Net) 2+

## License

[MIT](../LICENSE)
