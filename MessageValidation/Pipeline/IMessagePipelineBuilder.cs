namespace MessageValidation;

/// <summary>
/// Fluent builder for composing a <see cref="MessageDelegate"/> from a sequence
/// of middleware components — mirrors the ASP.NET Core <c>IApplicationBuilder</c> shape.
/// </summary>
public interface IMessagePipelineBuilder
{
    /// <summary>
    /// Gets the application-level <see cref="IServiceProvider"/> used to activate middleware
    /// when no per-message scope has been established yet.
    /// </summary>
    IServiceProvider ApplicationServices { get; }

    /// <summary>
    /// Adds a middleware delegate to the pipeline.
    /// </summary>
    IMessagePipelineBuilder Use(Func<MessageDelegate, MessageDelegate> middleware);

    /// <summary>
    /// Adds a strongly-typed middleware to the pipeline. The middleware is resolved from
    /// the per-message scope (<see cref="MessageContext.Services"/>) when available,
    /// otherwise from <see cref="ApplicationServices"/>.
    /// </summary>
    IMessagePipelineBuilder UseMiddleware<TMiddleware>() where TMiddleware : IMessageMiddleware;

    /// <summary>
    /// Branches the pipeline: when <paramref name="predicate"/> returns <see langword="true"/>,
    /// the <paramref name="branch"/> pipeline is executed and the remainder of the outer
    /// pipeline is skipped.
    /// </summary>
    IMessagePipelineBuilder Map(Func<MessageContext, bool> predicate, Action<IMessagePipelineBuilder> branch);

    /// <summary>
    /// Compiles the registered middleware into a single <see cref="MessageDelegate"/>.
    /// </summary>
    MessageDelegate Build();
}
