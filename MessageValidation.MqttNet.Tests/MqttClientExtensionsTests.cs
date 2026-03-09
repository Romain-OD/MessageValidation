using MQTTnet;
using MQTTnet.Client;
using NSubstitute;

namespace MessageValidation.MqttNet.Tests;

public class MqttClientExtensionsTests
{
    [Fact]
    public void UseMessageValidation_ReturnsSameClient()
    {
        var client = new MqttFactory().CreateMqttClient();
        var pipeline = Substitute.For<IMessageValidationPipeline>();

        var result = client.UseMessageValidation(pipeline);

        Assert.Same(client, result);
    }
}
