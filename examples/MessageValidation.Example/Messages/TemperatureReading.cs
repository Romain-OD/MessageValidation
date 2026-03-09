namespace MessageValidation.Example;

public class TemperatureReading
{
    public string SensorId { get; set; } = "";
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public class DeviceHeartbeat
{
    public string DeviceId { get; set; } = "";
    public string Status { get; set; } = "";
}
