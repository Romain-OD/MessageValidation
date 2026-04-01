# MessageValidation.Kafka

[Confluent Kafka](https://github.com/confluentinc/confluent-kafka-dotnet) transport adapter for the [MessageValidation](../MessageValidation/README.md) pipeline — automatically feed incoming Kafka messages into the validation pipeline with a single line of code.

## Installation

```bash
dotnet add package MessageValidation.Kafka
```

## Quick Start

### Option A — Extension method on `IConsumer<string, byte[]>`

Wire the pipeline directly onto an existing Confluent consumer:

```csharp
using Confluent.Kafka;
using MessageValidation.Kafka;

var config = new ConsumerConfig
{
    BootstrapServers = "broker.example.com:9092",
    GroupId = "my-service"
};

using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
var pipeline = serviceProvider.GetRequiredService<IMessageValidationPipeline>();

// Subscribe and start the consume loop — all messages go through the validation pipeline
await consumer.StartConsuming(pipeline, topics: ["sensors.temperature"], ct);
```

### Option B — DI registration

Let the DI container create and configure the consumer automatically:

```csharp
using MessageValidation;
using MessageValidation.Kafka;

builder.Services.AddMessageValidation(options =>
{
    options.MapSource<TemperatureReading>("sensors.temperature");
    options.DefaultFailureBehavior = FailureBehavior.Log;
});

builder.Services.AddMessageDeserializer<JsonMessageDeserializer>();
builder.Services.AddMessageHandler<TemperatureReading, TemperatureHandler>();

// Registers ConsumerConfig + IConsumer<string, byte[]>
builder.Services.AddKafkaMessageValidation(config =>
{
    config.BootstrapServers = "broker.example.com:9092";
    config.GroupId = "my-service";
});
```

Then inject `IConsumer<string, byte[]>` and start consuming in a `BackgroundService`:

```csharp
public class KafkaWorker(
    IConsumer<string, byte[]> consumer,
    IMessageValidationPipeline pipeline) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct) =>
        consumer.StartConsuming(pipeline, topics: ["sensors.temperature"], ct);
}
```

## Kafka Metadata

When the adapter creates a `MessageContext`, it populates the `Metadata` dictionary with Kafka-specific properties:

| Key | Type | Description |
|---|---|---|
| `kafka.topic` | `string` | Topic the message was consumed from |
| `kafka.partition` | `int` | Partition number |
| `kafka.offset` | `long` | Offset within the partition |
| `kafka.key` | `string` | Message key (empty string if null) |
| `kafka.timestamp` | `DateTime` | Message timestamp (UTC) |

Access them in your handler:

```csharp
public class TemperatureHandler : IMessageHandler<TemperatureReading>
{
    public Task HandleAsync(
        TemperatureReading message, MessageContext context, CancellationToken ct = default)
    {
        var partition = context.Metadata["kafka.partition"];
        var offset = context.Metadata["kafka.offset"];
        Console.WriteLine($"[partition={partition} offset={offset}] Sensor {message.SensorId}: {message.Value}°C");
        return Task.CompletedTask;
    }
}
```

## API Reference

### `IConsumer<string, byte[]>.StartConsuming(pipeline, ct)`

Starts a background consume loop (via `Task.Run`) that polls Kafka and passes every message through the pipeline. Exits gracefully when the cancellation token is cancelled. **Assumes the consumer is already subscribed.**

### `IConsumer<string, byte[]>.StartConsuming(pipeline, topics, ct)`

Calls `consumer.Subscribe(topics)` then starts the consume loop above.

### `AddKafkaMessageValidation(Action<ConsumerConfig>)`

Registers a singleton `ConsumerConfig` and a singleton `IConsumer<string, byte[]>` built from it. The consumer connects lazily on first use.

---

## Avro & Protobuf

> This is the main integration challenge when moving beyond JSON.

The adapter feeds the **raw `byte[]`** from Kafka directly into `IMessageDeserializer`. This works seamlessly for JSON. However, Confluent producers using Schema Registry embed a **wire-format prefix** before the actual encoded payload:

```
[0x00] [schema ID — 4 bytes, big-endian] [encoded payload]
```

For Protobuf there is an additional message-index byte (`0x00` for the first/only message type):

```
[0x00] [schema ID — 4 bytes] [0x00 message index] [protobuf bytes]
```

Your `IMessageDeserializer` receives these prefixed bytes as-is. The solution is a custom deserializer per format — no change to the adapter is required.

---

### JSON (no challenge)

Raw bytes are UTF-8 JSON — the standard `JsonMessageDeserializer` works without any changes.

```csharp
public class JsonMessageDeserializer : IMessageDeserializer
{
    public object Deserialize(byte[] payload, Type targetType) =>
        System.Text.Json.JsonSerializer.Deserialize(payload, targetType)
        ?? throw new InvalidOperationException($"Cannot deserialize to {targetType.Name}");
}
```

---

### Protobuf — strip the Confluent prefix

The Protobuf schema is baked into the generated class, so **no Schema Registry call is needed at runtime**. Strip the wire-format prefix and call `Google.Protobuf` directly:

```csharp
using Google.Protobuf;

public class ProtobufMessageDeserializer : IMessageDeserializer
{
    // magic(1) + schemaId(4) + messageIndex(1) = 6
    private const int PrefixLength = 6;

    public object Deserialize(byte[] payload, Type targetType)
    {
        var bytes = HasConfluentPrefix(payload) ? payload[PrefixLength..] : payload;

        var parser = (IMessage)Activator.CreateInstance(targetType)!;
        return parser.Descriptor.Parser.ParseFrom(bytes);
    }

    private static bool HasConfluentPrefix(byte[] payload) =>
        payload.Length > PrefixLength && payload[0] == 0x00;
}
```

```csharp
builder.Services.AddMessageDeserializer<ProtobufMessageDeserializer>();
```

---

### Avro — Schema Registry required

The Avro binary format is **schema-dependent**: the schema is NOT included in the payload. The schema must be fetched by ID from the Schema Registry to decode it. This is the key challenge.

Add the Schema Registry packages:

```bash
dotnet add package Confluent.SchemaRegistry
dotnet add package Confluent.SchemaRegistry.Serdes.Avro
```

Implement a deserializer that wraps Confluent's `AvroDeserializer<T>`:

```csharp
using System.Collections.Concurrent;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

public class AvroMessageDeserializer(ISchemaRegistryClient schemaRegistry) : IMessageDeserializer
{
    // Cache per type — avoids creating a new deserializer (and Schema Registry round-trip) per message
    private readonly ConcurrentDictionary<Type, object> _deserializers = new();

    public object Deserialize(byte[] payload, Type targetType)
    {
        var deserializer = _deserializers.GetOrAdd(targetType, t =>
        {
            var dt = typeof(AvroDeserializer<>).MakeGenericType(t);
            return Activator.CreateInstance(dt, schemaRegistry, (IEnumerable<KeyValuePair<string, string>>?)null)!;
        });

        var method = deserializer.GetType().GetMethod("Deserialize")!;
        return method.Invoke(deserializer, [payload, false, SerializationContext.Empty])!;
    }
}
```

Register the Schema Registry client and the deserializer:

```csharp
builder.Services.AddSingleton<ISchemaRegistryClient>(_ =>
    new CachedSchemaRegistryClient(new SchemaRegistryConfig
    {
        Url = "http://localhost:8081"
    }));

builder.Services.AddMessageDeserializer<AvroMessageDeserializer>();
```

> **Why cache deserializer instances?**
> `AvroDeserializer<T>` makes an HTTP call to Schema Registry to resolve schema IDs on first use. Caching it per type ensures each schema is fetched only once for the lifetime of the application.

---

## Requirements

- .NET 10+
- [MessageValidation](https://www.nuget.org/packages/MessageValidation) core package
- [Confluent.Kafka](https://www.nuget.org/packages/Confluent.Kafka) 2+

## License

[MIT](../LICENSE)