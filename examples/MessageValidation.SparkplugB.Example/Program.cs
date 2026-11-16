using MessageValidation;
using MessageValidation.Examples;
using MessageValidation.MqttNet;
using MessageValidation.SparkplugB;
using MessageValidation.SparkplugB.Example;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// NBIRTH + 3 NDATA + NDEATH are valid; a type change, a seq gap and a device_id on NDATA are rejected.
var demo = new Demo(expectedValid: 5, expectedInvalid: 3);

var builder = Host.CreateApplicationBuilder(args);
var broker = builder.Configuration["Broker"] ?? "localhost";

builder.Services
    .AddMessageValidation(
        options =>
        {
            options.MapSource<SparkplugBMessage>("spBv1.0/#");
            options.DefaultFailureBehavior = FailureBehavior.Custom;
        },
        pipeline =>
        {
            pipeline.UseSparkplugB(); // decodes the protobuf payload and parses the topic
            MessageValidationPipeline.ConfigureDefaults(pipeline);
        })
    .AddSparkplugBValidation()
    .AddDemo(demo)
    .AddMessageHandler<SparkplugBMessage, SparkplugMetricsHandler>()
    .AddMqttNetMessageValidation()
    .AddHostedService(sp => new SparkplugConsumer(sp.GetRequiredService<MQTTnet.IMqttClient>(), broker))
    .AddHostedService(sp => new EdgeNodeSimulator(broker));

using var host = builder.Build();
await host.StartAsync();
var exitCode = await demo.WaitAsync();
await host.StopAsync();
return exitCode;
