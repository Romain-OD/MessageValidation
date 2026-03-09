namespace MessageValidation.Example;

public class TemperatureHandler : IMessageHandler<TemperatureReading>
{
    public Task HandleAsync(TemperatureReading message, MessageContext context, CancellationToken ct = default)
    {
        Console.WriteLine($"  ✅ [{context.Source}] Sensor {message.SensorId}: {message.Value}°C at {message.Timestamp:HH:mm:ss}");
        return Task.CompletedTask;
    }
}

public class DeviceHeartbeatHandler : IMessageHandler<DeviceHeartbeat>
{
    public Task HandleAsync(DeviceHeartbeat message, MessageContext context, CancellationToken ct = default)
    {
        Console.WriteLine($"  ✅ [{context.Source}] Device {message.DeviceId} is {message.Status}");
        return Task.CompletedTask;
    }
}
