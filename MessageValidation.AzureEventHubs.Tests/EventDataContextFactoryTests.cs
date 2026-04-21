using Azure.Messaging.EventHubs;
using MessageValidation.AzureEventHubs;

namespace MessageValidation.AzureEventHubs.Tests;

public class EventDataContextFactoryTests
{
    private static EventData MakeEventData(
        byte[]? body = null,
        string? partitionKey = "pk-1",
        long sequenceNumber = 42L,
        long offset = 1024L,
        string messageId = "msg-1",
        string correlationId = "corr-1",
        string contentType = "application/json",
        IDictionary<string, object>? properties = null)
    {
#pragma warning disable CS0618 // long-offset overload is obsolete but the newer overload is not yet available in all SDK patch versions
        return EventHubsModelFactory.EventData(
            eventBody: new BinaryData(body ?? """{"SensorId":"room1","Value":22.5}"""u8.ToArray()),
            properties: properties ?? new Dictionary<string, object> { ["MessageType"] = "Telemetry" },
            partitionKey: partitionKey,
            sequenceNumber: sequenceNumber,
            offset: offset,
            enqueuedTime: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
#pragma warning restore CS0618
    }

    [Fact]
    public void CreateContext_UsesEventHubNameAsSource()
    {
        var data = MakeEventData();
        data.MessageId = "m-1";

        var ctx = EventDataContextFactory.CreateContext(data, eventHubName: "telemetry", partitionId: "0");

        Assert.Equal("telemetry", ctx.Source);
    }

    [Fact]
    public void CreateContext_CopiesPayloadBytes()
    {
        var body = """{"SensorId":"x","Value":1}"""u8.ToArray();
        var data = MakeEventData(body: body);

        var ctx = EventDataContextFactory.CreateContext(data, eventHubName: "telemetry", partitionId: "0");

        Assert.Equal(body, ctx.RawPayload);
    }

    [Fact]
    public void CreateContext_PopulatesEventHubsMetadata()
    {
        var data = MakeEventData(
            partitionKey: "device-42",
            sequenceNumber: 99L,
            offset: 2048L);
        data.MessageId = "msg-9";
        data.CorrelationId = "corr-9";
        data.ContentType = "application/json";

        var ctx = EventDataContextFactory.CreateContext(data, eventHubName: "telemetry", partitionId: "3");

        Assert.Equal("telemetry", ctx.Metadata["eventhubs.eventHubName"]);
        Assert.Equal("3", ctx.Metadata["eventhubs.partitionId"]);
        Assert.Equal("device-42", ctx.Metadata["eventhubs.partitionKey"]);
        Assert.Equal(99L, ctx.Metadata["eventhubs.sequenceNumber"]);
        Assert.Equal(2048L, ctx.Metadata["eventhubs.offset"]);
        Assert.Equal("msg-9", ctx.Metadata["eventhubs.messageId"]);
        Assert.Equal("corr-9", ctx.Metadata["eventhubs.correlationId"]);
        Assert.Equal("application/json", ctx.Metadata["eventhubs.contentType"]);
        Assert.IsType<DateTime>(ctx.Metadata["eventhubs.enqueuedTime"]);

        var props = Assert.IsAssignableFrom<IDictionary<string, object>>(ctx.Metadata["eventhubs.properties"]);
        Assert.Equal("Telemetry", props["MessageType"]);
    }

    [Fact]
    public void CreateContext_NullData_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            EventDataContextFactory.CreateContext(null!, eventHubName: "telemetry", partitionId: "0"));
    }

    [Fact]
    public void CreateContext_EmptyEventHubName_Throws()
    {
        var data = MakeEventData();

        Assert.Throws<ArgumentException>(() =>
            EventDataContextFactory.CreateContext(data, eventHubName: "", partitionId: "0"));
    }
}
