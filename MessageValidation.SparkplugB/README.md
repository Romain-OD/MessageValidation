# MessageValidation.SparkplugB

[Eclipse Sparkplug B](https://sparkplug.eclipse.org/) validator for the [MessageValidation](../README.md) pipeline. It parses `spBv1.0` topics and Protocol Buffers payloads, then rejects messages that break the spec before they reach your handlers.

## Installation

```bash
dotnet add package MessageValidation.SparkplugB
dotnet add package MessageValidation.MqttNet   # or any MQTT transport adapter
```

## Quick Start

```csharp
services
    .AddMessageValidation(
        options => options.MapSource<SparkplugBMessage>("spBv1.0/#"),
        pipeline =>
        {
            pipeline.UseSparkplugB();                              // topic + protobuf → SparkplugBMessage
            MessageValidationPipeline.ConfigureDefaults(pipeline); // validate → failure handling → handler
        })
    .AddSparkplugBValidation()                                     // seq/bdSeq/birth tracking on by default
    .AddMessageHandler<SparkplugBMessage, MyHandler>()
    .AddMqttNetMessageValidation();
```

Handlers receive only valid messages:

```csharp
public sealed class MyHandler : IMessageHandler<SparkplugBMessage>
{
    public Task HandleAsync(SparkplugBMessage msg, MessageContext ctx, CancellationToken ct = default)
    {
        // msg.Topic.GroupId / EdgeNodeId / DeviceId / MessageType
        // msg.Payload.Metrics, msg.Payload.Seq …
        return Task.CompletedTask;
    }
}
```

Invalid messages follow your configured `FailureBehavior` (Log, DeadLetter, Skip, Throw, Custom), the same as any other MessageValidation adapter.

### Standalone

```csharp
var validator = new SparkplugBValidator();
var result = validator.Validate(topic, payloadBytes);
```

## What is validated

| Rule | Details |
|---|---|
| Topic | `spBv1.0/<group_id>/<message_type>/<edge_node_id>[/<device_id>]`. Device messages (`D*`) require a `device_id`; node messages (`N*`) must not have one. IDs must be non-empty and contain no `+` or `#`. |
| Payload | The payload must be valid Sparkplug B protobuf. |
| `timestamp` | Required on every message except NDEATH. |
| `seq` | Required (0–255) on NBIRTH, DBIRTH, NDATA, DDATA and DDEATH. NDEATH must not include it. |
| `bdSeq` | NBIRTH and NDEATH must carry a 64-bit `bdSeq` metric. |
| `Node Control/Rebirth` | NBIRTH must carry this Boolean metric. |
| Birth metrics | Every metric needs a `name` and a `datatype`. |
| Data metrics | Every metric needs a `name` or an `alias`. |
| DataType | The value field must match the declared datatype (e.g. `Double` → `double_value`). `UInt8`/`UInt16` values are range-checked, and a metric with `is_null` must not carry a value. |

### Session tracking (`AddSparkplugBValidation(trackSessionState: true)`, the default)

Session state is tracked per edge node (`group_id` + `edge_node_id`):

- **seq:** each message must be `(previous + 1) % 256`, rolling over from 255 to 0. After a gap, tracking resyncs to the received value.
- **No NBIRTH yet:** data is rejected until an NBIRTH is seen, because the spec says the host must request a rebirth.
- **Datatypes:** they are remembered from NBIRTH/DBIRTH, so NDATA/DDATA metrics sent by alias only are still type-checked.
- **Stale NDEATH:** an NDEATH whose `bdSeq` doesn't match the current NBIRTH is reported as stale (it is an old MQTT Will).

State is held in memory per process. If several consumers share the same edge node, turn tracking off (`trackSessionState: false`) or keep state elsewhere.

### Not covered (yet)

- `STATE` host messages (`spBv1.0/STATE/<host_id>`)
- Deep DataSet/Template validation

## Notes

- `UseSparkplugB()` must be registered before `ConfigureDefaults`. The core deserialization stage skips messages that are already parsed.
- `AddSparkplugBValidation()` registers a fallback `IMessageDeserializer` if you haven't registered one, so Sparkplug-only apps don't need a JSON deserializer.
- `Proto/sparkplug_b.proto` comes from [Eclipse Tahu](https://github.com/eclipse-tahu/tahu) and is licensed under EPL-2.0 (only a `csharp_namespace` option was added). The rest of the package is MIT.

See [`examples/MessageValidation.SparkplugB.Example`](../examples/MessageValidation.SparkplugB.Example) for a runnable worker.
