using System.Text.Json;
using Example.ImmediateValidation;
using MessageValidation;
using MessageValidation.ImmediateValidations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ─── Build the DI container ────────────────────────────────────────────
var services = new ServiceCollection();

services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

services.AddMessageValidation(options =>
{
    options.MapSource<TemperatureReading>("sensors/+/temperature");
    options.MapSource<DeviceHeartbeat>("devices/#");
    options.DefaultFailureBehavior = FailureBehavior.Log;
});

services.AddMessageDeserializer<JsonMessageDeserializer>();

// Single line: every message type implementing IValidationTarget<T>
// is now validated via the Immediate.Validations source-generated
// Validate() method (zero reflection, AOT-friendly).
services.AddMessageImmediateValidations();

services.AddMessageHandler<TemperatureReading, TemperatureHandler>();
services.AddMessageHandler<DeviceHeartbeat, DeviceHeartbeatHandler>();

await using var sp = services.BuildServiceProvider();

var pipeline = sp.GetRequiredService<IMessageValidationPipeline>();

// ─── Simulate incoming messages ────────────────────────────────────────

Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("  MessageValidation.ImmediateValidations — Example");
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine();

// 1. Valid temperature reading (matches sensors/+/temperature)
Console.WriteLine("=> Valid temperature reading from sensors/kitchen/temperature:");
await pipeline.ProcessAsync(CreateContext("sensors/kitchen/temperature", new TemperatureReading
{
    SensorId = "kitchen-01",
    Value = 22.5,
    Timestamp = DateTime.UtcNow
}));
Console.WriteLine();

// 2. Invalid temperature reading (empty SensorId, value out of range)
Console.WriteLine("=> Invalid temperature reading (empty SensorId, value=999):");
await pipeline.ProcessAsync(CreateContext("sensors/bedroom/temperature", new TemperatureReading
{
    SensorId = "",
    Value = 999,
    Timestamp = DateTime.UtcNow
}));
Console.WriteLine();

// 3. Valid heartbeat (matches devices/#)
Console.WriteLine("=> Valid heartbeat from devices/thermostat-01/status:");
await pipeline.ProcessAsync(CreateContext("devices/thermostat-01/status", new DeviceHeartbeat
{
    DeviceId = "thermostat-01",
    Status = "online"
}));
Console.WriteLine();

// 4. Invalid heartbeat (unknown status)
Console.WriteLine("=> Invalid heartbeat (Status='broken'):");
await pipeline.ProcessAsync(CreateContext("devices/floor2/sensor-hub/status", new DeviceHeartbeat
{
    DeviceId = "sensor-hub-42",
    Status = "broken"
}));
Console.WriteLine();

Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("  Done.");
Console.WriteLine("══════════════════════════════════════════════════════");

static MessageContext CreateContext<T>(string source, T message) => new()
{
    Source = source,
    RawPayload = JsonSerializer.SerializeToUtf8Bytes(message)
};
