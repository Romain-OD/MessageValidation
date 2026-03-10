using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageValidation.DataAnnotations.Tests;

public class DataAnnotationsIntegrationTests
{
    [Fact]
    public void AddMessageDataAnnotationsValidation_RegistersOpenGenericAdapter()
    {
        var services = new ServiceCollection();
        services.AddMessageDataAnnotationsValidation();

        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IMessageValidator<TestMessage>>());
    }

    [Fact]
    public async Task FullPipeline_ValidMessage_IsHandled()
    {
        var handled = false;

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<IMessageDeserializer>(new JsonDeserializer());

        services.AddMessageValidation(options =>
        {
            options.MapSource<TestMessage>("test/topic");
        });

        services.AddMessageDataAnnotationsValidation();
        services.AddScoped<IMessageHandler<TestMessage>>(_ => new CallbackHandler(() => handled = true));

        var pipeline = services.BuildServiceProvider().GetRequiredService<MessageValidationPipeline>();

        var payload = JsonSerializer.SerializeToUtf8Bytes(new TestMessage { Name = "hello", Value = 10 });
        var context = new MessageContext { Source = "test/topic", RawPayload = payload };

        await pipeline.ProcessAsync(context);

        Assert.True(handled);
    }

    [Fact]
    public async Task FullPipeline_InvalidMessage_IsNotHandled()
    {
        var handled = false;

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<IMessageDeserializer>(new JsonDeserializer());

        services.AddMessageValidation(options =>
        {
            options.MapSource<TestMessage>("test/topic");
        });

        services.AddMessageDataAnnotationsValidation();
        services.AddScoped<IMessageHandler<TestMessage>>(_ => new CallbackHandler(() => handled = true));

        var pipeline = services.BuildServiceProvider().GetRequiredService<MessageValidationPipeline>();

        var payload = JsonSerializer.SerializeToUtf8Bytes(new TestMessage { Name = "", Value = 0 });
        var context = new MessageContext { Source = "test/topic", RawPayload = payload };

        await pipeline.ProcessAsync(context);

        Assert.False(handled);
    }

    private class JsonDeserializer : IMessageDeserializer
    {
        public object Deserialize(byte[] payload, Type targetType) =>
            JsonSerializer.Deserialize(payload, targetType)!;
    }

    private class CallbackHandler(Action onHandle) : IMessageHandler<TestMessage>
    {
        public Task HandleAsync(TestMessage message, MessageContext context, CancellationToken ct = default)
        {
            onHandle();
            return Task.CompletedTask;
        }
    }
}
