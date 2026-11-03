using Google.Protobuf;
using MessageValidation.SparkplugB.Protobuf;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Metric = MessageValidation.SparkplugB.Protobuf.Payload.Types.Metric;

namespace MessageValidation.SparkplugB.Example;

/// <summary>Runs only for messages that passed Sparkplug B validation.</summary>
public sealed class SparkplugMetricsHandler(ILogger<SparkplugMetricsHandler> logger) : IMessageHandler<SparkplugBMessage>
{
    public Task HandleAsync(SparkplugBMessage message, MessageContext context, CancellationToken ct = default)
    {
        var metrics = string.Join(", ", message.Payload.Metrics.Select(m =>
            $"{(m.HasName ? m.Name : "#" + m.Alias)}={m.ValueCase switch
            {
                Metric.ValueOneofCase.DoubleValue => m.DoubleValue.ToString("0.0"),
                Metric.ValueOneofCase.BooleanValue => m.BooleanValue.ToString(),
                Metric.ValueOneofCase.LongValue => m.LongValue.ToString(),
                var other => other.ToString(),
            }}"));

        logger.LogInformation("✔ {Type} seq={Seq} from {Edge}: {Metrics}",
            message.Topic.MessageType, message.Payload.Seq, message.Topic.EdgeNodeId, metrics);
        return Task.CompletedTask;
    }
}

/// <summary>Connects the pipeline-wired MQTT client and subscribes to the Sparkplug namespace.</summary>
public sealed class SparkplugConsumer(IMqttClient client, string broker) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer(broker).WithClientId("host-app").Build(), ct);
        await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter("spBv1.0/#").Build(), ct);
    }
}

/// <summary>A fake edge node publishing a good session, then a few spec violations.</summary>
public sealed class EdgeNodeSimulator(string broker) : BackgroundService
{
    private const ulong BdSeq = 0;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var client = new MqttClientFactory().CreateMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer(broker)
            .WithClientId("edge-node")
            .WithWillTopic(Topic("NDEATH"))          // broker publishes NDEATH if we crash
            .WithWillPayload(Death().ToByteArray())
            .Build(), ct);

        await Task.Delay(1000, ct); // let the host app subscribe

        ulong seq = 0;
        await Publish(client, "NBIRTH", new Payload
        {
            Timestamp = Now(), Seq = seq,
            Metrics =
            {
                new Metric { Name = "bdSeq", Datatype = (uint)DataType.Int64, LongValue = BdSeq },
                new Metric { Name = "Node Control/Rebirth", Datatype = (uint)DataType.Boolean, BooleanValue = false },
                new Metric { Name = "Temperature", Alias = 1, Datatype = (uint)DataType.Double, DoubleValue = 21.0 },
                new Metric { Name = "Running", Alias = 2, Datatype = (uint)DataType.Boolean, BooleanValue = true },
            },
        }, ct);

        for (var i = 0; i < 3; i++)
            await Publish(client, "NDATA", Data(++seq, new Metric { Alias = 1, DoubleValue = 21.5 + i }), ct);

        // ✘ Temperature was born as Double; a string is a type violation.
        await Publish(client, "NDATA", Data(++seq, new Metric { Alias = 1, StringValue = "hot" }), ct);

        // ✘ seq jumps by 2: a message was lost.
        seq += 2;
        await Publish(client, "NDATA", Data(seq, new Metric { Alias = 1, DoubleValue = 23.0 }), ct);

        // ✘ NDATA is a node message: device_id is not allowed.
        await Publish(client, "NDATA", Data(++seq, new Metric { Alias = 2, BooleanValue = false }), ct,
            topic: Topic("NDATA") + "/Pump1");

        await Publish(client, "NDEATH", Death(), ct);
        await client.DisconnectAsync(cancellationToken: ct);
    }

    private static Task Publish(IMqttClient client, string type, Payload payload, CancellationToken ct, string? topic = null) =>
        Task.WhenAll(
            client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic ?? Topic(type))
                .WithPayload(payload.ToByteArray())
                .Build(), ct),
            Task.Delay(1000, ct)); // pace the demo

    private static string Topic(string type) => $"spBv1.0/Plant1/{type}/Line1-PLC";

    private static Payload Data(ulong seq, Metric metric) => new() { Timestamp = Now(), Seq = seq, Metrics = { metric } };

    private static Payload Death() => new()
    {
        Metrics = { new Metric { Name = "bdSeq", Datatype = (uint)DataType.Int64, LongValue = BdSeq } },
    };

    private static ulong Now() => (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
