using System.Text.Json;
using MessageValidation;
using MessageValidation.Example;
using MessageValidation.FluentValidation;
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
services.AddMessageFluentValidation(typeof(TemperatureReadingValidator).Assembly);
services.AddMessageHandler<TemperatureReading, TemperatureHandler>();
services.AddMessageHandler<DeviceHeartbeat, DeviceHeartbeatHandler>();

await using var sp = services.BuildServiceProvider();

var pipeline = sp.GetRequiredService<IMessageValidationPipeline>();

// ─── Simulate incoming messages ────────────────────────────────────────

Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("  MessageValidation — Example Pipeline");
Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine();

// 1. Valid temperature reading (exact wildcard match: sensors/+/temperature)
Console.WriteLine("→ Valid temperature reading from sensors/kitchen/temperature:");
await pipeline.ProcessAsync(CreateContext("sensors/kitchen/temperature", new TemperatureReading
{
    SensorId = "kitchen-01",
    Value = 22.5,
    Timestamp = DateTime.UtcNow
}));
Console.WriteLine();

// 2. Invalid temperature reading (missing SensorId, value out of range)
Console.WriteLine("→ Invalid temperature reading (missing SensorId, value=999):");
await pipeline.ProcessAsync(CreateContext("sensors/bedroom/temperature", new TemperatureReading
{
    SensorId = "",
    Value = 999,
    Timestamp = DateTime.UtcNow
}));
Console.WriteLine();

// 3. Valid device heartbeat (multi-level wildcard match: devices/#)
Console.WriteLine("→ Valid heartbeat from devices/thermostat-01/status:");
await pipeline.ProcessAsync(CreateContext("devices/thermostat-01/status", new DeviceHeartbeat
{
    DeviceId = "thermostat-01",
    Status = "online"
}));
Console.WriteLine();

// 4. Unknown source — no mapping
Console.WriteLine("→ Unknown source (logs/system/error — no mapping registered):");
await pipeline.ProcessAsync(CreateContext("logs/system/error", new { Message = "something" }));
Console.WriteLine();

// 5. Another wildcard match at a deeper level
Console.WriteLine("→ Valid heartbeat from devices/floor2/sensor-hub/battery:");
await pipeline.ProcessAsync(CreateContext("devices/floor2/sensor-hub/battery", new DeviceHeartbeat
{
    DeviceId = "sensor-hub-42",
    Status = "offline"
}));
Console.WriteLine();

Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine("  Done. Check the output above for pipeline results.");
Console.WriteLine("══════════════════════════════════════════════════════");

// ─── Helper ────────────────────────────────────────────────────────────

static MessageContext CreateContext<T>(string source, T message) => new()
{
    Source = source,
    RawPayload = JsonSerializer.SerializeToUtf8Bytes(message)
};
