using MessageValidation;
using MessageValidation.Examples;
using MessageValidation.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

const string Queue = "temperature";
var demo = new Demo(expectedValid: 2, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<TemperatureReading>(Queue); // Source = routing key
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    .AddSingleton<IMessageValidator<TemperatureReading>, TemperatureReadingValidator>()
    .AddDemo(demo)
    .AddRabbitMqMessageValidation(factory => factory.HostName = Demo.Env("RABBITMQ_HOST", "localhost"))
    .BuildServiceProvider();

var connection = await Demo.RetryAsync(() => Task.FromResult(services.GetRequiredService<IConnection>()));
await using var channel = await connection.CreateChannelAsync();
await channel.QueueDeclareAsync(Queue, durable: false, exclusive: false, autoDelete: true);

await channel.UseMessageValidation(services.GetRequiredService<IMessageValidationPipeline>(), Queue);

foreach (var reading in TemperatureReading.Samples)
    await channel.BasicPublishAsync(exchange: string.Empty, routingKey: Queue, body: Demo.Json(reading));

return await demo.WaitAsync();
