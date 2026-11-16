namespace MessageValidation.Example;

public record TemperatureReading
{
    public string SensorId { get; set; } = "";
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public record DeviceHeartbeat
{
    public string DeviceId { get; set; } = "";
    public string Status { get; set; } = "";
}
