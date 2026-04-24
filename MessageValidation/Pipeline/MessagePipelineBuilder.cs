using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation;

/// <summary>
/// Default <see cref="IMessagePipelineBuilder"/> implementation.
/// Middlewares are composed in reverse registration order at <see cref="Build"/> time,
/// so the first registered middleware runs first.
/// </summary>
public sealed class MessagePipelineBuilder : IMessagePipelineBuilder
{
    private readonly List<Func<MessageDelegate, MessageDelegate>> _components = new();

    public MessagePipelineBuilder(IServiceProvider applicationServices)
    {
        ApplicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
    }

    public IServiceProvider ApplicationServices { get; }

    public IMessagePipelineBuilder Use(Func<MessageDelegate, MessageDelegate> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _components.Add(middleware);
        return this;
    }

    public IMessagePipelineBuilder UseMiddleware<TMiddleware>() where TMiddleware : IMessageMiddleware
    {
        return Use(next => async (ctx, ct) =>
        {
            var sp = ctx.Services ?? ApplicationServices;
            var middleware = ActivatorUtilities.GetServiceOrCreateInstance<TMiddleware>(sp);
            await middleware.InvokeAsync(ctx, next, ct).ConfigureAwait(false);
        });
    }

    public IMessagePipelineBuilder Map(Func<MessageContext, bool> predicate, Action<IMessagePipelineBuilder> branch)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(branch);

        var branchBuilder = new MessagePipelineBuilder(ApplicationServices);
        branch(branchBuilder);
        var branchDelegate = branchBuilder.Build();

        return Use(next => async (ctx, ct) =>
        {
            if (predicate(ctx))
                await branchDelegate(ctx, ct).ConfigureAwait(false);
            else
                await next(ctx, ct).ConfigureAwait(false);
        });
    }

    public MessageDelegate Build()
    {
        MessageDelegate app = static (_, _) => Task.CompletedTask;
        for (var i = _components.Count - 1; i >= 0; i--)
            app = _components[i](app);
        return app;
    }
}
