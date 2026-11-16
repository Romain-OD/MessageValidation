using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using MessageValidation;
using MessageValidation.AzureEventHubs;
using MessageValidation.Examples;
using Microsoft.Extensions.DependencyInjection;

// Hub and consumer group declared in MessageValidation.IntegrationTests/Infrastructure/EventHubs/Config.json.
const string Hub = "integration-hub";
var connectionString = Demo.Env("EVENTHUBS_CONNECTION_STRING",
    "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;");
var demo = new Demo(expectedValid: 2, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<TemperatureReading>(Hub); // Source = event hub name
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    .AddSingleton<IMessageValidator<TemperatureReading>, TemperatureReadingValidator>()
    .AddDemo(demo)
    .AddAzureEventHubsMessageValidation(connectionString, Hub, consumerGroup: "integration-group")
    .BuildServiceProvider();

await using (var producer = new EventHubProducerClient(connectionString, Hub))
{
    await Demo.RetryAsync(() => producer.SendAsync(TemperatureReading.Samples.Select(r => new EventData(Demo.Json(r)))));
}

using var stop = new CancellationTokenSource();
var consuming = services.GetRequiredService<EventHubConsumerClient>()
    .StartConsuming(services.GetRequiredService<IMessageValidationPipeline>(), startReadingAtEarliestEvent: true, stop.Token);

var exitCode = await demo.WaitAsync();
await stop.CancelAsync();
await consuming;
return exitCode;
