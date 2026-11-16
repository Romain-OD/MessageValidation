using Confluent.Kafka;
using MessageValidation;
using MessageValidation.Examples;
using MessageValidation.Kafka;
using Microsoft.Extensions.DependencyInjection;

const string Topic = "temperature";
var bootstrapServers = Demo.Env("KAFKA_BOOTSTRAP_SERVERS", "localhost:9092");
var demo = new Demo(expectedValid: 2, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<TemperatureReading>(Topic); // Source = topic
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    .AddSingleton<IMessageValidator<TemperatureReading>, TemperatureReadingValidator>()
    .AddDemo(demo)
    .AddKafkaMessageValidation(consumer =>
    {
        consumer.BootstrapServers = bootstrapServers;
        consumer.GroupId = "message-validation-example";
        consumer.AutoOffsetReset = AutoOffsetReset.Earliest;
    })
    .BuildServiceProvider();

using (var producer = new ProducerBuilder<string, byte[]>(new ProducerConfig { BootstrapServers = bootstrapServers }).Build())
{
    foreach (var reading in TemperatureReading.Samples)
    {
        await Demo.RetryAsync(() => producer.ProduceAsync(Topic,
            new Message<string, byte[]> { Key = reading.SensorId, Value = Demo.Json(reading) }));
    }
}

var consumer = services.GetRequiredService<IConsumer<string, byte[]>>();
using var stop = new CancellationTokenSource();
var consuming = consumer.StartConsuming(services.GetRequiredService<IMessageValidationPipeline>(), [Topic], stop.Token);

var exitCode = await demo.WaitAsync();
await stop.CancelAsync();
await consuming;
consumer.Close();
return exitCode;
