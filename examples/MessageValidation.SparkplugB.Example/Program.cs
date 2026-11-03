using MessageValidation;
using MessageValidation.MqttNet;
using MessageValidation.SparkplugB;
using MessageValidation.SparkplugB.Example;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var broker = builder.Configuration["Broker"] ?? "localhost";

builder.Services
    .AddMessageValidation(
        options =>
        {
            options.MapSource<SparkplugBMessage>("spBv1.0/#");
            options.DefaultFailureBehavior = FailureBehavior.Log;
        },
        pipeline =>
        {
            pipeline.UseSparkplugB();
            MessageValidationPipeline.ConfigureDefaults(pipeline);
        })
    .AddSparkplugBValidation()
    .AddMessageHandler<SparkplugBMessage, SparkplugMetricsHandler>()
    .AddMqttNetMessageValidation()
    .AddHostedService(sp => new SparkplugConsumer(sp.GetRequiredService<MQTTnet.IMqttClient>(), broker))
    .AddHostedService(sp => new EdgeNodeSimulator(broker));

await builder.Build().RunAsync();
