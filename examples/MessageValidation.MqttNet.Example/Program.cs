using MessageValidation;
using MessageValidation.Examples;
using MessageValidation.MqttNet;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;

var demo = new Demo(expectedValid: 2, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<TemperatureReading>("sensors/+/temperature");
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    .AddSingleton<IMessageValidator<TemperatureReading>, TemperatureReadingValidator>()
    .AddDemo(demo)
    .AddMqttNetMessageValidation() // registers an IMqttClient whose received messages flow into the pipeline
    .BuildServiceProvider();

// Distinct client ids: a broker disconnects the older session when an id is reused.
MqttClientOptions Options(string clientId) =>
    new MqttClientOptionsBuilder().WithTcpServer(Demo.Env("MQTT_HOST", "localhost")).WithClientId(clientId).Build();

var subscriber = services.GetRequiredService<IMqttClient>();
await Demo.RetryAsync(() => subscriber.ConnectAsync(Options("validator")));
await subscriber.SubscribeAsync("sensors/+/temperature");

using var publisher = new MqttClientFactory().CreateMqttClient();
await publisher.ConnectAsync(Options("sensor-gateway"));

for (var i = 0; i < TemperatureReading.Samples.Length; i++)
{
    await publisher.PublishAsync(new MqttApplicationMessageBuilder()
        .WithTopic($"sensors/device-{i}/temperature")
        .WithPayload(Demo.Json(TemperatureReading.Samples[i]))
        .Build());
}

return await demo.WaitAsync();
