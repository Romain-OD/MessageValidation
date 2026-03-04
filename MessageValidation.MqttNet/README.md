# MessageValidation.MqttNet

[MQTTnet](https://github.com/dotnet/MQTTnet) transport adapter for the [MessageValidation](../MessageValidation/README.md) pipeline — automatically feed incoming MQTT messages into the validation pipeline with a single line of code.

## Installation

```bash
dotnet add package MessageValidation.MqttNet
```

## Quick Start

### Option A — Extension method on `IMqttClient`

Wire the pipeline directly onto an existing MQTTnet client:

```csharp
using MessageValidation.MqttNet;
using MQTTnet;
using MQTTnet.Client;

var factory = new MqttFactory();
var client = factory.CreateMqttClient();

var pipeline = serviceProvider.GetRequiredService<MessageValidationPipeline>();

// One line — all incoming messages now go through the validation pipeline
client.UseMessageValidation(pipeline);

// Connect and subscribe as usual
await client.ConnectAsync(new MqttClientOptionsBuilder()
    .WithTcpServer("broker.example.com")
    .Build());

await client.SubscribeAsync("sensors/+/temperature");
```

### Option B — DI registration

Let the DI container create and configure the client automatically:

```csharp
using MessageValidation;
using MessageValidation.MqttNet;

builder.Services.AddMessageValidation(options =>
{
    options.MapSource<TemperatureReading>("sensors/+/temperature");
    options.DefaultFailureBehavior = FailureBehavior.Log;
});

builder.Services.AddMessageDeserializer<JsonMessageDeserializer>();
builder.Services.AddMessageHandler<TemperatureReading, TemperatureHandler>();

// Register an IMqttClient with the validation pipeline pre-wired
builder.Services.AddMqttNetMessageValidation();
```

Then inject `IMqttClient` wherever you need it:

```csharp
public class MqttWorker(IMqttClient mqttClient) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await mqttClient.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer("broker.example.com")
            .Build(), ct);

        await mqttClient.SubscribeAsync("sensors/+/temperature", cancellationToken: ct);

        // Messages are automatically deserialized, validated, and dispatched
        await Task.Delay(Timeout.Infinite, ct);
    }
}
```

### Server-side validation

You can also hook the pipeline on an MQTTnet **server** to validate messages as they are published:

```csharp
using MessageValidation.MqttNet;
using MQTTnet.Server;

var serverOptions = new MqttServerOptionsBuilder()
    .WithDefaultEndpoint()
    .Build();

var server = new MqttFactory().CreateMqttServer(serverOptions);

var pipeline = serviceProvider.GetRequiredService<MessageValidationPipeline>();
server.UseMessageValidation(pipeline);

await server.StartAsync();
```

## MQTT Metadata

When the adapter creates a `MessageContext`, it populates the `Metadata` dictionary with MQTT-specific properties:

| Key | Type | Description |
|---|---|---|
| `mqtt.qos` | `MqttQualityOfServiceLevel` | Quality of Service level |
| `mqtt.retain` | `bool` | Whether the message is retained |
| `mqtt.clientId` | `string` | Client ID (server-side only) |
| `mqtt.contentType` | `string` | MQTT 5.0 content type |
| `mqtt.responseTopic` | `string` | MQTT 5.0 response topic |

Access them in your handler:

```csharp
public class TemperatureHandler : IMessageHandler<TemperatureReading>
{
    public Task HandleAsync(
        TemperatureReading message, MessageContext context, CancellationToken ct = default)
    {
        var qos = context.Metadata["mqtt.qos"];
        Console.WriteLine($"[{context.Source}] QoS={qos} — Sensor {message.SensorId}: {message.Value}°C");
        return Task.CompletedTask;
    }
}
```

## API Reference

### `IMqttClient.UseMessageValidation(MessageValidationPipeline pipeline)`

Hooks the pipeline into the client's `ApplicationMessageReceivedAsync` event. Returns the same `IMqttClient` for chaining.

### `MqttServer.UseMessageValidation(MessageValidationPipeline pipeline)`

Hooks the pipeline into the server's `InterceptingPublishAsync` event. Returns the same `MqttServer` for chaining.

### `AddMqttNetMessageValidation(Action<IMqttClient>? configureMqttClient = null)`

Registers a singleton `IMqttClient` in the DI container with the validation pipeline pre-wired. Optional callback for additional client configuration.

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) core package
- [MQTTnet](https://www.nuget.org/packages/MQTTnet) 4+

## License

[MIT](../LICENSE)
