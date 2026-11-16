using Example.ImmediateValidation;
using MessageValidation;
using MessageValidation.Examples;
using MessageValidation.ImmediateValidations;
using Microsoft.Extensions.DependencyInjection;

var demo = new Demo(expectedValid: 2, expectedInvalid: 2);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<TemperatureReading>("sensors/+/temperature");
        options.MapSource<DeviceHeartbeat>("devices/#");
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    // Every [Validate] type is checked by its source-generated Validate() method: no reflection, AOT-friendly.
    .AddMessageImmediateValidations()
    .AddDemo(demo)
    .BuildServiceProvider();

var pipeline = services.GetRequiredService<IMessageValidationPipeline>();

await pipeline.ProcessAsync(Demo.Context("sensors/kitchen/temperature",
    new TemperatureReading { SensorId = "kitchen-01", Value = 22.5, Timestamp = DateTime.UtcNow }));
await pipeline.ProcessAsync(Demo.Context("sensors/bedroom/temperature",
    new TemperatureReading { SensorId = "", Value = 999, Timestamp = DateTime.UtcNow }));
await pipeline.ProcessAsync(Demo.Context("devices/thermostat-01/status",
    new DeviceHeartbeat { DeviceId = "thermostat-01", Status = "online" }));
await pipeline.ProcessAsync(Demo.Context("devices/floor2/sensor-hub/status",
    new DeviceHeartbeat { DeviceId = "sensor-hub-42", Status = "broken" }));

return await demo.WaitAsync();
