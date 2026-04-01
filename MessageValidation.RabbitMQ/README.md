# MessageValidation.RabbitMQ

[RabbitMQ](https://www.rabbitmq.com/) transport adapter for the [MessageValidation](../MessageValidation/README.md) pipeline — automatically feed incoming RabbitMQ messages into the validation pipeline with a single line of code.

## Installation

```bash
dotnet add package MessageValidation.RabbitMQ
```

## Quick Start

### Option A — Extension method on `IChannel`

Wire the pipeline directly onto an existing RabbitMQ channel:

```csharp
using MessageValidation.RabbitMQ;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
var connection = await factory.CreateConnectionAsync();
var channel = await connection.CreateChannelAsync();

var pipeline = serviceProvider.GetRequiredService<IMessageValidationPipeline>();

// One line — all consumed messages now go through the validation pipeline
var consumerTag = await channel.UseMessageValidation(pipeline, queue: "sensor-readings");
```

### Option B — DI registration

Register the RabbitMQ connection factory via DI:

```csharp
using MessageValidation;
using MessageValidation.RabbitMQ;

builder.Services.AddMessageValidation(options =>
{
    options.MapSource<TemperatureReading>("sensor.temperature");
    options.DefaultFailureBehavior = FailureBehavior.Log;
});

builder.Services.AddMessageDeserializer<JsonMessageDeserializer>();
builder.Services.AddMessageHandler<TemperatureReading, TemperatureHandler>();

// Register the RabbitMQ connection
builder.Services.AddRabbitMqMessageValidation(factory =>
{
    factory.HostName = "localhost";
    factory.UserName = "guest";
    factory.Password = "guest";
});
```

Then create channels and wire them to the pipeline:

```csharp
public class RabbitMqWorker(IConnection connection, IMessageValidationPipeline pipeline) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.QueueDeclareAsync("sensor-readings", durable: true, exclusive: false,
            autoDelete: false, cancellationToken: ct);

        // Messages are automatically deserialized, validated, and dispatched
        await channel.UseMessageValidation(pipeline, queue: "sensor-readings", ct: ct);

        await Task.Delay(Timeout.Infinite, ct);
    }
}
```

## RabbitMQ Metadata

When the adapter creates a `MessageContext`, it populates the `Metadata` dictionary with RabbitMQ-specific properties:

| Key | Type | Description |
|---|---|---|
| `rabbitmq.exchange` | `string` | The exchange the message was published to |
| `rabbitmq.routingKey` | `string` | The routing key |
| `rabbitmq.deliveryTag` | `ulong` | The delivery tag for ack/nack |
| `rabbitmq.redelivered` | `bool` | Whether the message is a redelivery |
| `rabbitmq.consumerTag` | `string` | The consumer tag |
| `rabbitmq.contentType` | `string` | Content type (if set) |
| `rabbitmq.correlationId` | `string` | Correlation ID (if set) |
| `rabbitmq.messageId` | `string` | Message ID (if set) |
| `rabbitmq.headers` | `IDictionary<string, object?>` | Message headers (if set) |

Access them in your handler:

```csharp
public class TemperatureHandler : IMessageHandler<TemperatureReading>
{
    public Task HandleAsync(
        TemperatureReading message, MessageContext context, CancellationToken ct = default)
    {
        var routingKey = context.Metadata["rabbitmq.routingKey"];
        Console.WriteLine($"[{routingKey}] Sensor {message.SensorId}: {message.Value}°C");
        return Task.CompletedTask;
    }
}
```

## API Reference

### `IChannel.UseMessageValidation(IMessageValidationPipeline pipeline, string queue, bool autoAck = true, CancellationToken ct = default)`

Creates an `AsyncEventingBasicConsumer` wired to the pipeline and starts consuming from the specified queue. Returns the consumer tag.

### `AddRabbitMqMessageValidation(Action<ConnectionFactory> configureFactory)`

Registers a `ConnectionFactory` and singleton `IConnection` in the DI container.

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) core package
- [RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client) 7+

## License

[MIT](../LICENSE)
