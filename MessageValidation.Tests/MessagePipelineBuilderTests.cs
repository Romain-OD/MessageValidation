using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.Tests;

public class MessagePipelineBuilderTests
{
    private static IServiceProvider EmptyServices() => new ServiceCollection().BuildServiceProvider();

    [Fact]
    public async Task Use_PreservesRegistrationOrder()
    {
        var trace = new List<string>();
        var builder = new MessagePipelineBuilder(EmptyServices());

        builder.Use(next => async (ctx, ct) => { trace.Add("A-before"); await next(ctx, ct); trace.Add("A-after"); });
        builder.Use(next => async (ctx, ct) => { trace.Add("B-before"); await next(ctx, ct); trace.Add("B-after"); });
        builder.Use(next => (ctx, ct) => { trace.Add("C"); return Task.CompletedTask; });

        var pipeline = builder.Build();
        await pipeline(TestHelpers.CreateContext("t", System.Array.Empty<byte>()), default);

        Assert.Equal(new[] { "A-before", "B-before", "C", "B-after", "A-after" }, trace);
    }

    [Fact]
    public async Task Use_ShortCircuits_WhenNextNotInvoked()
    {
        var trace = new List<string>();
        var builder = new MessagePipelineBuilder(EmptyServices());

        builder.Use(next => (ctx, ct) => { trace.Add("stop"); return Task.CompletedTask; });
        builder.Use(next => (ctx, ct) => { trace.Add("never"); return Task.CompletedTask; });

        await builder.Build()(TestHelpers.CreateContext("t", System.Array.Empty<byte>()), default);

        Assert.Equal(new[] { "stop" }, trace);
    }

    [Fact]
    public async Task Map_RunsBranch_WhenPredicateTrue_AndSkipsOuterRemainder()
    {
        var trace = new List<string>();
        var builder = new MessagePipelineBuilder(EmptyServices());

        builder.Map(
            ctx => ctx.Source == "match",
            branch => branch.Use(next => (ctx, ct) => { trace.Add("branch"); return Task.CompletedTask; }));
        builder.Use(next => (ctx, ct) => { trace.Add("outer"); return Task.CompletedTask; });

        await builder.Build()(TestHelpers.CreateContext("match", System.Array.Empty<byte>()), default);

        Assert.Equal(new[] { "branch" }, trace);
    }

    [Fact]
    public async Task Map_SkipsBranch_WhenPredicateFalse_AndContinuesOuter()
    {
        var trace = new List<string>();
        var builder = new MessagePipelineBuilder(EmptyServices());

        builder.Map(
            ctx => ctx.Source == "match",
            branch => branch.Use(next => (ctx, ct) => { trace.Add("branch"); return Task.CompletedTask; }));
        builder.Use(next => (ctx, ct) => { trace.Add("outer"); return Task.CompletedTask; });

        await builder.Build()(TestHelpers.CreateContext("other", System.Array.Empty<byte>()), default);

        Assert.Equal(new[] { "outer" }, trace);
    }

    [Fact]
    public async Task Map_SupportsNesting()
    {
        var trace = new List<string>();
        var builder = new MessagePipelineBuilder(EmptyServices());

        builder.Map(
            ctx => ctx.Source.StartsWith("a/"),
            outer => outer.Map(
                ctx => ctx.Source == "a/b",
                inner => inner.Use(next => (ctx, ct) => { trace.Add("a/b"); return Task.CompletedTask; })));

        var pipeline = builder.Build();

        await pipeline(TestHelpers.CreateContext("a/b", System.Array.Empty<byte>()), default);
        await pipeline(TestHelpers.CreateContext("a/c", System.Array.Empty<byte>()), default);
        await pipeline(TestHelpers.CreateContext("x", System.Array.Empty<byte>()), default);

        Assert.Equal(new[] { "a/b" }, trace);
    }

    [Fact]
    public async Task UseMiddleware_ResolvesFromContextServices_WhenAvailable()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TrackingMiddleware>();
        var sp = services.BuildServiceProvider();

        var builder = new MessagePipelineBuilder(EmptyServices());
        builder.UseMiddleware<TrackingMiddleware>();

        var ctx = TestHelpers.CreateContext("t", System.Array.Empty<byte>());
        ctx.Services = sp;
        await builder.Build()(ctx, default);

        Assert.Equal(1, sp.GetRequiredService<TrackingMiddleware>().Calls);
    }

    [Fact]
    public async Task UseMiddleware_FallsBackToApplicationServices_WhenContextServicesNull()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TrackingMiddleware>();
        var sp = services.BuildServiceProvider();

        var builder = new MessagePipelineBuilder(sp);
        builder.UseMiddleware<TrackingMiddleware>();

        await builder.Build()(TestHelpers.CreateContext("t", System.Array.Empty<byte>()), default);

        Assert.Equal(1, sp.GetRequiredService<TrackingMiddleware>().Calls);
    }

    [Fact]
    public void Constructor_NullApplicationServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new MessagePipelineBuilder(null!));
    }

    private sealed class TrackingMiddleware : IMessageMiddleware
    {
        public int Calls { get; private set; }

        public Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct)
        {
            Calls++;
            return next(context, ct);
        }
    }
}
