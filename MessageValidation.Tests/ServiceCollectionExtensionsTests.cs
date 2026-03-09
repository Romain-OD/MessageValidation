using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace MessageValidation.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageValidation_RegistersPipelineAndOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageDeserializer>(new JsonTestDeserializer());

        services.AddMessageValidation(options =>
        {
            options.MapSource<TestMessage>("test/topic");
        });

        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<MessageValidationOptions>());
        Assert.NotNull(sp.GetService<MessageValidationPipeline>());
    }

    [Fact]
    public void AddMessageHandler_RegistersHandler()
    {
        var services = new ServiceCollection();
        services.AddMessageHandler<TestMessage, TestHandler>();

        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IMessageHandler<TestMessage>>());
    }

    [Fact]
    public void AddValidationFailureHandler_RegistersHandler()
    {
        var services = new ServiceCollection();
        services.AddValidationFailureHandler<TestFailureHandler>();

        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IValidationFailureHandler>());
    }

    [Fact]
    public void AddMessageDeserializer_RegistersDeserializer()
    {
        var services = new ServiceCollection();
        services.AddMessageDeserializer<JsonTestDeserializer>();

        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IMessageDeserializer>());
    }

    private class TestHandler : IMessageHandler<TestMessage>
    {
        public Task HandleAsync(TestMessage message, MessageContext context, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private class TestFailureHandler : IValidationFailureHandler
    {
        public Task HandleAsync(MessageValidationResult result, MessageContext context, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}
