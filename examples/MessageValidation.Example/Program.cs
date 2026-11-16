using MessageValidation;
using MessageValidation.Examples;
using Microsoft.Extensions.DependencyInjection;

// Core package only: wildcard source mapping, a hand-written IMessageValidator<T>,
// inline middleware, and a custom IValidationFailureHandler (FailureBehavior.Custom).
var demo = new Demo(expectedValid: 2, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(
        options =>
        {
            options.MapSource<TemperatureReading>("sensors/+/temperature"); // single-level wildcard
            options.MapSource<TemperatureReading>("archive/#");              // multi-level wildcard
            options.DefaultFailureBehavior = FailureBehavior.Custom;
        },
        pipeline =>
        {
            pipeline.Use(next => async (context, ct) =>
            {
                Console.WriteLine($"→ {context.Source} ({context.RawPayload.Length} bytes)");
                await next(context, ct);
            });
            MessageValidationPipeline.ConfigureDefaults(pipeline);
        })
    .AddSingleton<IMessageValidator<TemperatureReading>, TemperatureReadingValidator>()
    .AddDemo(demo)
    .BuildServiceProvider();

var pipeline = services.GetRequiredService<IMessageValidationPipeline>();
var (kitchen, garage, broken) = (TemperatureReading.Samples[0], TemperatureReading.Samples[1], TemperatureReading.Samples[2]);

await pipeline.ProcessAsync(Demo.Context("sensors/kitchen/temperature", kitchen));
await pipeline.ProcessAsync(Demo.Context("archive/2026/11/garage", garage));
await pipeline.ProcessAsync(Demo.Context("sensors/attic/temperature", broken));
await pipeline.ProcessAsync(Demo.Context("logs/system/error", kitchen)); // no mapping: ignored by the pipeline

return await demo.WaitAsync();
