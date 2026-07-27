using MessageValidation.MqttNet;
using MQTTnet;
using Testcontainers.Mosquitto;

namespace MessageValidation.IntegrationTests;

public sealed class MqttIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PublishedMessage_ReachesValidationPipeline()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await using var container = new MosquittoBuilder("eclipse-mosquitto:2.0.22").Build();
        await container.StartAsync(timeout.Token);

        var subscriberOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(container.Hostname, container.MqttPort)
            .Build();
        var publisherOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(container.Hostname, container.MqttPort)
            .Build();

        var clientFactory = new MqttClientFactory();
        using var subscriber = clientFactory.CreateMqttClient();
        using var publisher = clientFactory.CreateMqttClient();

        var pipeline = new RecordingPipeline();
        subscriber.UseMessageValidation(pipeline);

        await subscriber.ConnectAsync(subscriberOptions, timeout.Token);
        await publisher.ConnectAsync(publisherOptions, timeout.Token);

        var topic = $"message-validation/{Guid.NewGuid():N}";
        await subscriber.SubscribeAsync(topic, cancellationToken: timeout.Token);

        var payload = """{"transport":"mqtt"}"""u8.ToArray();
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        await publisher.PublishAsync(message, timeout.Token);

        var context = await pipeline.WaitAsync(timeout.Token);

        Assert.Equal(topic, context.Source);
        Assert.Equal(payload, context.RawPayload);
        Assert.Equal(message.QualityOfServiceLevel, context.Metadata["mqtt.qos"]);
    }
}
