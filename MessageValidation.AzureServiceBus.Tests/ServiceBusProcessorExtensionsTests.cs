using Azure.Messaging.ServiceBus;
using MessageValidation.AzureServiceBus;

namespace MessageValidation.AzureServiceBus.Tests;

public class ServiceBusProcessorExtensionsTests
{
    private static ServiceBusReceivedMessage MakeMessage(
        string? subject = null,
        byte[]? body = null,
        string messageId = "m-1",
        string correlationId = "corr-1",
        string contentType = "application/json",
        string sessionId = "",
        int deliveryCount = 1)
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: new BinaryData(body ?? """{"OrderId":42}"""u8.ToArray()),
            messageId: messageId,
            correlationId: correlationId,
            subject: subject,
            contentType: contentType,
            sessionId: sessionId,
            enqueuedTime: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            deliveryCount: deliveryCount);
    }

    [Fact]
    public void CreateContext_UsesSubjectAsSource_WhenPresent()
    {
        var message = MakeMessage(subject: "order.created");

        var ctx = ServiceBusProcessorExtensions.CreateContext(message, entityPath: "orders");

        Assert.Equal("order.created", ctx.Source);
    }

    [Fact]
    public void CreateContext_FallsBackToEntityPath_WhenSubjectMissing()
    {
        var message = MakeMessage(subject: null);

        var ctx = ServiceBusProcessorExtensions.CreateContext(message, entityPath: "orders");

        Assert.Equal("orders", ctx.Source);
    }

    [Fact]
    public void CreateContext_CopiesPayloadBytes()
    {
        var body = """{"OrderId":7}"""u8.ToArray();
        var message = MakeMessage(body: body);

        var ctx = ServiceBusProcessorExtensions.CreateContext(message, entityPath: "orders");

        Assert.Equal(body, ctx.RawPayload);
    }

    [Fact]
    public void CreateContext_PopulatesServiceBusMetadata()
    {
        var message = MakeMessage(
            subject: "order.created",
            messageId: "msg-1",
            correlationId: "corr-9",
            contentType: "application/json",
            sessionId: "sess-1",
            deliveryCount: 3);

        var ctx = ServiceBusProcessorExtensions.CreateContext(message, entityPath: "orders");

        Assert.Equal("orders", ctx.Metadata["servicebus.entityPath"]);
        Assert.Equal("msg-1", ctx.Metadata["servicebus.messageId"]);
        Assert.Equal("order.created", ctx.Metadata["servicebus.subject"]);
        Assert.Equal("corr-9", ctx.Metadata["servicebus.correlationId"]);
        Assert.Equal("application/json", ctx.Metadata["servicebus.contentType"]);
        Assert.Equal("sess-1", ctx.Metadata["servicebus.sessionId"]);
        Assert.Equal(3, ctx.Metadata["servicebus.deliveryCount"]);
        Assert.IsType<DateTime>(ctx.Metadata["servicebus.enqueuedTime"]);
    }

    [Fact]
    public void CreateContext_NullMessage_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => ServiceBusProcessorExtensions.CreateContext(null!, entityPath: "orders"));
    }
}
