using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MessageValidation.Tests;

public class MessageValidationPipelineTests
{
    private static ServiceProvider BuildProvider(
        Action<MessageValidationOptions> configure,
        Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddMetrics();

        var options = new MessageValidationOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<MessageValidationMetrics>();
        services.AddSingleton<IMessageDeserializer>(new JsonTestDeserializer());

        configureServices?.Invoke(services);

        services.AddSingleton<MessageValidationPipeline>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ProcessAsync_NoMapping_DoesNotThrow()
    {
        await using var sp = BuildProvider(o => { });
        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("unknown/topic", new TestMessage { Name = "test" });

        await pipeline.ProcessAsync(context);
    }

    [Fact]
    public async Task ProcessAsync_ValidMessage_InvokesHandler()
    {
        var handler = Substitute.For<IMessageHandler<TestMessage>>();

        await using var sp = BuildProvider(
            o => o.MapSource<TestMessage>("test/topic"),
            s => s.AddScoped(_ => handler));

        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("test/topic", new TestMessage { Name = "hello", Value = 42 });

        await pipeline.ProcessAsync(context);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestMessage>(m => m.Name == "hello" && m.Value == 42),
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ValidMessage_NoValidator_InvokesHandler()
    {
        var handler = Substitute.For<IMessageHandler<TestMessage>>();

        await using var sp = BuildProvider(
            o => o.MapSource<TestMessage>("test/topic"),
            s => s.AddScoped(_ => handler));

        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("test/topic", new TestMessage { Name = "hello" });

        await pipeline.ProcessAsync(context);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestMessage>(m => m.Name == "hello"),
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_InvalidMessage_DoesNotInvokeHandler()
    {
        var handler = Substitute.For<IMessageHandler<TestMessage>>();

        await using var sp = BuildProvider(
            o => o.MapSource<TestMessage>("test/topic"),
            s =>
            {
                s.AddScoped(_ =>
                {
                    var validator = Substitute.For<IMessageValidator<TestMessage>>();
                    validator.ValidateAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
                        .Returns(MessageValidationResult.Failure([new MessageValidationError("Name", "Required")]));
                    return validator;
                });
                s.AddScoped(_ => handler);
            });

        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("test/topic", new TestMessage { Name = "" });

        await pipeline.ProcessAsync(context);

        await handler.DidNotReceive().HandleAsync(
            Arg.Any<TestMessage>(),
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ThrowException_Behavior_ThrowsOnValidationFailure()
    {
        await using var sp = BuildProvider(
            o =>
            {
                o.MapSource<TestMessage>("test/topic");
                o.DefaultFailureBehavior = FailureBehavior.ThrowException;
            },
            s => s.AddScoped(_ =>
            {
                var validator = Substitute.For<IMessageValidator<TestMessage>>();
                validator.ValidateAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
                    .Returns(MessageValidationResult.Failure([new MessageValidationError("Name", "Required")]));
                return validator;
            }));

        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("test/topic", new TestMessage { Name = "" });

        var ex = await Assert.ThrowsAsync<MessageValidationException>(
            () => pipeline.ProcessAsync(context));

        Assert.False(ex.ValidationResult.IsValid);
        Assert.Contains("Name", ex.Message);
    }

    [Fact]
    public async Task ProcessAsync_CustomFailureBehavior_InvokesFailureHandler()
    {
        var failureHandler = Substitute.For<IValidationFailureHandler>();

        await using var sp = BuildProvider(
            o =>
            {
                o.MapSource<TestMessage>("test/topic");
                o.DefaultFailureBehavior = FailureBehavior.Custom;
            },
            s =>
            {
                s.AddScoped(_ =>
                {
                    var validator = Substitute.For<IMessageValidator<TestMessage>>();
                    validator.ValidateAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
                        .Returns(MessageValidationResult.Failure([new MessageValidationError("Name", "Required")]));
                    return validator;
                });
                s.AddScoped(_ => failureHandler);
            });

        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("test/topic", new TestMessage { Name = "" });

        await pipeline.ProcessAsync(context);

        await failureHandler.Received(1).HandleAsync(
            Arg.Any<MessageValidationResult>(),
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WildcardSource_ResolvesAndHandles()
    {
        var handler = Substitute.For<IMessageHandler<TestMessage>>();

        await using var sp = BuildProvider(
            o => o.MapSource<TestMessage>("sensors/+/data"),
            s => s.AddScoped(_ => handler));

        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("sensors/room1/data", new TestMessage { Name = "temp" });

        await pipeline.ProcessAsync(context);

        await handler.Received(1).HandleAsync(
            Arg.Is<TestMessage>(m => m.Name == "temp"),
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_SkipBehavior_DoesNotInvokeHandlerOrThrow()
    {
        var handler = Substitute.For<IMessageHandler<TestMessage>>();

        await using var sp = BuildProvider(
            o =>
            {
                o.MapSource<TestMessage>("test/topic");
                o.DefaultFailureBehavior = FailureBehavior.Skip;
            },
            s =>
            {
                s.AddScoped(_ =>
                {
                    var validator = Substitute.For<IMessageValidator<TestMessage>>();
                    validator.ValidateAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
                        .Returns(MessageValidationResult.Failure([new MessageValidationError("Name", "Required")]));
                    return validator;
                });
                s.AddScoped(_ => handler);
            });

        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("test/topic", new TestMessage { Name = "" });

        await pipeline.ProcessAsync(context);

        await handler.DidNotReceive().HandleAsync(
            Arg.Any<TestMessage>(),
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_DeadLetterBehavior_InvokesFailureHandler()
    {
        var failureHandler = Substitute.For<IValidationFailureHandler>();

        await using var sp = BuildProvider(
            o =>
            {
                o.MapSource<TestMessage>("test/topic");
                o.DefaultFailureBehavior = FailureBehavior.DeadLetter;
            },
            s =>
            {
                s.AddScoped(_ =>
                {
                    var validator = Substitute.For<IMessageValidator<TestMessage>>();
                    validator.ValidateAsync(Arg.Any<TestMessage>(), Arg.Any<CancellationToken>())
                        .Returns(MessageValidationResult.Failure([new MessageValidationError("Name", "Required")]));
                    return validator;
                });
                s.AddScoped(_ => failureHandler);
            });

        var pipeline = sp.GetRequiredService<MessageValidationPipeline>();
        var context = TestHelpers.CreateContext("test/topic", new TestMessage { Name = "" });

        await pipeline.ProcessAsync(context);

        await failureHandler.Received(1).HandleAsync(
            Arg.Any<MessageValidationResult>(),
            Arg.Any<MessageContext>(),
            Arg.Any<CancellationToken>());
    }
}
