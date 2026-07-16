using NATS.Client.Core;
using NSubstitute;

namespace MessageValidation.NatsNet.Tests;

public class NatsConnectionExtensionsTests
{
    [Fact]
    public async Task SubscribeWithMessageValidationAsync_NullConnection_ThrowsArgumentNullException()
    {
        INatsConnection connection = null!;
        var pipeline = Substitute.For<IMessageValidationPipeline>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            connection.SubscribeWithMessageValidationAsync("subject", pipeline));
    }

    [Fact]
    public async Task SubscribeWithMessageValidationAsync_NullSubject_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<INatsConnection>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            connection.SubscribeWithMessageValidationAsync(null!, pipeline));
    }

    [Fact]
    public async Task SubscribeWithMessageValidationAsync_EmptySubject_ThrowsArgumentException()
    {
        var connection = Substitute.For<INatsConnection>();
        var pipeline = Substitute.For<IMessageValidationPipeline>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            connection.SubscribeWithMessageValidationAsync(string.Empty, pipeline));
    }

    [Fact]
    public async Task SubscribeWithMessageValidationAsync_NullPipeline_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<INatsConnection>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            connection.SubscribeWithMessageValidationAsync("subject", null!));
    }
}
