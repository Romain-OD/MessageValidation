using Azure.Messaging.ServiceBus;
using MessageValidation;
using MessageValidation.AzureServiceBus;
using MessageValidation.Examples;
using Microsoft.Extensions.DependencyInjection;

// Queue declared in MessageValidation.IntegrationTests/Infrastructure/ServiceBus/Config.json (mounted into the emulator).
const string Queue = "integration-queue";
var connectionString = Demo.Env("SERVICEBUS_CONNECTION_STRING",
    "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;");
var demo = new Demo(expectedValid: 2, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<TemperatureReading>("sensors/temperature"); // Source = Subject (falls back to the queue name)
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    .AddSingleton<IMessageValidator<TemperatureReading>, TemperatureReadingValidator>()
    .AddDemo(demo)
    .AddAzureServiceBusMessageValidation(connectionString)
    .BuildServiceProvider();

var client = services.GetRequiredService<ServiceBusClient>();

await using var sender = client.CreateSender(Queue);
foreach (var reading in TemperatureReading.Samples)
{
    await Demo.RetryAsync(() => sender.SendMessageAsync(
        new ServiceBusMessage(Demo.Json(reading)) { Subject = "sensors/temperature", ContentType = "application/json" }));
}

await using var processor = client.CreateProcessor(Queue);
processor.UseMessageValidation(services.GetRequiredService<IMessageValidationPipeline>(), args =>
{
    Console.WriteLine($"Service Bus error: {args.Exception.Message}");
    return Task.CompletedTask;
});
await processor.StartProcessingAsync();

var exitCode = await demo.WaitAsync();
await processor.StopProcessingAsync();
return exitCode;
