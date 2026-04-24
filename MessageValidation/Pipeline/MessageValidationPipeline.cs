using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation;

/// <summary>
/// Default implementation of <see cref="IMessageValidationPipeline"/>.
/// Composes a middleware chain (type resolution → deserialization → validation →
/// failure handling → handler dispatch, wrapped by metrics) using
/// <see cref="MessagePipelineBuilder"/>, and invokes it for every incoming message
/// inside a fresh DI scope.
/// </summary>
/// <remarks>
/// Registered automatically by <see cref="ServiceCollectionExtensions.AddMessageValidation"/>.
/// Do not instantiate directly; resolve <see cref="IMessageValidationPipeline"/> from the DI container.
/// </remarks>
public sealed class MessageValidationPipeline : IMessageValidationPipeline
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MessageDelegate _pipeline;

    public MessageValidationPipeline(
        IServiceScopeFactory scopeFactory,
        IServiceProvider applicationServices,
        Action<IMessagePipelineBuilder>? configurePipeline = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        var builder = new MessagePipelineBuilder(applicationServices);
        if (configurePipeline is not null)
        {
            configurePipeline(builder);
        }
        else
        {
            ConfigureDefaults(builder);
        }

        _pipeline = builder.Build();
    }

    /// <summary>
    /// Appends the built-in middleware chain to <paramref name="builder"/>.
    /// Exposed so custom configurations can insert their own middleware before or after
    /// the defaults.
    /// </summary>
    public static void ConfigureDefaults(IMessagePipelineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder
            .UseMiddleware<MetricsMiddleware>()
            .UseMiddleware<TypeResolutionMiddleware>()
            .UseMiddleware<DeserializationMiddleware>()
            .UseMiddleware<ValidationMiddleware>()
            .UseMiddleware<FailureHandlingMiddleware>()
            .UseMiddleware<HandlerDispatchMiddleware>();
    }

    public async Task ProcessAsync(MessageContext context, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        context.Services = scope.ServiceProvider;
        try
        {
            await _pipeline(context, ct).ConfigureAwait(false);
        }
        finally
        {
            context.Services = null;
        }
    }
}
