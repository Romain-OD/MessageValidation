using MessageValidation;
using MessageValidation.Examples;
using MessageValidation.OnnxRuntime;
using Microsoft.Extensions.DependencyInjection;

// max_score.onnx scores a message as max(features): a stand-in for a real on-device anomaly model.
var demo = new Demo(expectedValid: 2, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<TemperatureReading>("sensors/+/temperature");
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    .AddMessageOnnxValidation<TemperatureReading>(
        Path.Combine(AppContext.BaseDirectory, "max_score.onnx"),
        reading => [(float)(reading.Value / 100)],
        threshold: 0.5f) // above 50 °C is an anomaly
    .AddDemo(demo)
    .BuildServiceProvider();

var pipeline = services.GetRequiredService<IMessageValidationPipeline>();

foreach (var reading in TemperatureReading.Samples)
    await pipeline.ProcessAsync(Demo.Context("sensors/edge-01/temperature", reading));

return await demo.WaitAsync();
