using MessageValidation;
using MessageValidation.Examples;
using MessageValidation.NatsNet;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

const string Subject = "sensors.temperature";
var demo = new Demo(expectedValid: 2, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<TemperatureReading>(Subject); // Source = subject
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    .AddSingleton<IMessageValidator<TemperatureReading>, TemperatureReadingValidator>()
    .AddDemo(demo)
    .AddNatsNetMessageValidation(Demo.Env("NATS_URL", "nats://localhost:4222"))
    .BuildServiceProvider();

var nats = services.GetRequiredService<INatsConnection>();
await Demo.RetryAsync(async () => await nats.ConnectAsync());

using var stop = new CancellationTokenSource();
var subscription = nats.SubscribeWithMessageValidationAsync(
    Subject, services.GetRequiredService<IMessageValidationPipeline>(), ct: stop.Token);
await nats.PingAsync(); // round-trip so the server has registered the subscription

foreach (var reading in TemperatureReading.Samples)
    await nats.PublishAsync(Subject, Demo.Json(reading));

var exitCode = await demo.WaitAsync();
await stop.CancelAsync();
try
{
    await subscription;
}
catch (OperationCanceledException)
{
}

return exitCode;
