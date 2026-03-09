using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;

namespace MessageValidation.MqttNet.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMqttNetMessageValidation_RegistersMqttClient()
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<IMessageDeserializer>(new TestDeserializer());
        services.AddMessageValidation(options =>
        {
            options.MapSource<TestMsg>("test/topic");
        });

        services.AddMqttNetMessageValidation();

        var sp = services.BuildServiceProvider();
        var client = sp.GetService<IMqttClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddMqttNetMessageValidation_InvokesConfigureCallback()
    {
        var callbackInvoked = false;

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<IMessageDeserializer>(new TestDeserializer());
        services.AddMessageValidation(options =>
        {
            options.MapSource<TestMsg>("test/topic");
        });

        services.AddMqttNetMessageValidation(client =>
        {
            callbackInvoked = true;
        });

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<IMqttClient>();

        Assert.True(callbackInvoked);
    }
}

public class TestMsg
{
    public string Data { get; set; } = "";
}

public class TestDeserializer : IMessageDeserializer
{
    public object Deserialize(byte[] payload, Type targetType) =>
        System.Text.Json.JsonSerializer.Deserialize(payload, targetType)!;
}
