# Examples

One runnable project per NuGet package. Each one sends 2 to 5 valid and 1 to 3 invalid messages through the pipeline, prints `✔`/`✘` per message, and exits **0 (PASS)** or **1 (FAIL)**, so every example also works as a smoke test.

| NuGet package | Example | Infrastructure (podman) |
|---|---|---|
| MessageValidation | [MessageValidation.Example](MessageValidation.Example) — wildcards, inline middleware, custom failure handler | none |
| MessageValidation.DataAnnotations | [DataAnnotations.Example](MessageValidation.DataAnnotations.Example) | none |
| MessageValidation.FluentValidation | [FluentValidation.Example](MessageValidation.FluentValidation.Example) | none |
| MessageValidation.ImmediateValidations | [ImmediateValidations.Example](MessageValidation.ImmediateValidations.Example) | none |
| MessageValidation.OnnxRuntime | [OnnxRuntime.Example](MessageValidation.OnnxRuntime.Example): anomaly score from an ONNX model | none |
| MessageValidation.MqttNet | [MqttNet.Example](MessageValidation.MqttNet.Example) | Mosquitto |
| MessageValidation.SparkplugB | [SparkplugB.Example](MessageValidation.SparkplugB.Example): simulated edge node | Mosquitto |
| MessageValidation.RabbitMQ | [RabbitMQ.Example](MessageValidation.RabbitMQ.Example) | RabbitMQ |
| MessageValidation.Kafka | [Kafka.Example](MessageValidation.Kafka.Example) | Kafka (KRaft, [kafka/Dockerfile](kafka/Dockerfile)) |
| MessageValidation.NatsNet | [NatsNet.Example](MessageValidation.NatsNet.Example) | NATS |
| MessageValidation.AzureServiceBus | [AzureServiceBus.Example](MessageValidation.AzureServiceBus.Example) | Service Bus emulator + SQL Server |
| MessageValidation.AzureEventHubs | [AzureEventHubs.Example](MessageValidation.AzureEventHubs.Example) | Event Hubs emulator + Azurite |

Shared code is in [Shared/](Shared):
- `Demo` tracks the outcomes; it also provides the handler, the failure handler and the JSON deserializer.
- `TemperatureReading` holds the sample messages and a hand-written validator.

## Run in podman (or docker)

```powershell
pwsh examples/run-all.ps1                      # all 12, prints a PASS/FAIL table
pwsh examples/run-all.ps1 kafka natsnet        # a subset
pwsh examples/run-all.ps1 -Engine docker
```

Or run a single example with compose. Its broker starts automatically.

```bash
cd examples
podman build -t localhost/messagevalidation-examples -f Dockerfile ..
podman build -t localhost/messagevalidation-kafka:3.9.1 kafka
podman compose run --rm example-rabbitmq
podman compose down
```

The Azure emulators need about 2 GB of RAM (`podman machine set --memory 4096` if needed). By running them, you accept their EULAs (`ACCEPT_EULA=Y`).

**Behind a TLS-inspecting proxy?** If the image build fails with `UntrustedRoot` during `dotnet restore`, export the proxy's root CA in PEM format to `examples/certs/<name>.crt`. That folder is git-ignored.

## Run without containers

The first five examples need no infrastructure:

```bash
dotnet run --project examples/MessageValidation.FluentValidation.Example
```

The transport examples read `MQTT_HOST`, `RABBITMQ_HOST`, `KAFKA_BOOTSTRAP_SERVERS`, `NATS_URL`, `SERVICEBUS_CONNECTION_STRING` and `EVENTHUBS_CONNECTION_STRING`. Each defaults to a broker on `localhost`.
