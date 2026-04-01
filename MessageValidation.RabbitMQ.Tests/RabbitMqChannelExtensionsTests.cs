using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MessageValidation.RabbitMQ.Tests;

public class RabbitMqChannelExtensionsTests
{
    [Fact]
    public async Task UseMessageValidation_ReturnsConsumerTag()
    {
        var channel = Substitute.For<IChannel>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();

        channel.BasicConsumeAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IAsyncBasicConsumer>(),
            Arg.Any<CancellationToken>())
            .Returns("test-consumer-tag");

        var tag = await channel.UseMessageValidation(pipeline, "test-queue");

        Assert.Equal("test-consumer-tag", tag);
    }

    [Fact]
    public async Task UseMessageValidation_PassesAutoAckToBasicConsume()
    {
        var channel = Substitute.For<IChannel>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();

        channel.BasicConsumeAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IAsyncBasicConsumer>(),
            Arg.Any<CancellationToken>())
            .Returns("tag");

        await channel.UseMessageValidation(pipeline, "test-queue", autoAck: false);

        await channel.Received(1).BasicConsumeAsync(
            Arg.Is<string>("test-queue"),
            Arg.Is<bool>(false),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<IDictionary<string, object>>(),
            Arg.Any<IAsyncBasicConsumer>(),
            Arg.Any<CancellationToken>());
    }
}
