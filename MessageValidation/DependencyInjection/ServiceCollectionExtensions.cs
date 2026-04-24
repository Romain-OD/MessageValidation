using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MessageValidation;

/// <summary>
/// Extension methods for registering the <c>MessageValidation</c> pipeline and
/// its components (<see cref="IMessageDeserializer"/>, <see cref="IMessageValidator{TMessage}"/>,
/// <see cref="IMessageHandler{TMessage}"/>, <see cref="IDeadLetterHandler"/>, and
/// <see cref="IValidationFailureHandler"/>) into a <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core <see cref="IMessageValidationPipeline"/> and configures
    /// source-to-type mappings via <see cref="MessageValidationOptions"/>.
    /// Call this once, then register a deserializer, validators, and handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">
    /// Callback to configure <see cref="MessageValidationOptions"/>, including
    /// <see cref="MessageValidationOptions.MapSource{TMessage}"/> mappings and
    /// <see cref="MessageValidationOptions.DefaultFailureBehavior"/>.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMessageValidation(options =&gt;
    /// {
    ///     options.MapSource&lt;TemperatureReading&gt;("sensors/+/temperature");
    ///     options.DefaultFailureBehavior = FailureBehavior.DeadLetter;
    /// });
    /// services.AddMessageDeserializer&lt;JsonMessageDeserializer&gt;();
    /// services.AddMessageHandler&lt;TemperatureReading, TemperatureHandler&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddMessageValidation(
        this IServiceCollection services,
        Action<MessageValidationOptions> configure)
    {
        var options = new MessageValidationOptions();
        configure(options);

        services.AddLogging();
        services.AddMetrics();
        services.AddSingleton(options);
        services.TryAddSingleton<MessageValidationMetrics>();
        services.AddSingleton<MessageValidationPipeline>(sp =>
            new MessageValidationPipeline(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp,
                configurePipeline: null));
        services.TryAddSingleton<IMessageValidationPipeline>(sp => sp.GetRequiredService<MessageValidationPipeline>());

        return services;
    }

    /// <summary>
    /// Adds the core <see cref="IMessageValidationPipeline"/> and lets the caller compose
    /// a custom middleware pipeline via <paramref name="configurePipeline"/>. Call
    /// <see cref="MessageValidationPipeline.ConfigureDefaults"/> from the callback to keep
    /// the built-in stages.
    /// </summary>
    public static IServiceCollection AddMessageValidation(
        this IServiceCollection services,
        Action<MessageValidationOptions> configure,
        Action<IMessagePipelineBuilder> configurePipeline)
    {
        ArgumentNullException.ThrowIfNull(configurePipeline);

        var options = new MessageValidationOptions();
        configure(options);

        services.AddLogging();
        services.AddMetrics();
        services.AddSingleton(options);
        services.TryAddSingleton<MessageValidationMetrics>();
        services.AddSingleton<MessageValidationPipeline>(sp =>
            new MessageValidationPipeline(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp,
                configurePipeline));
        services.TryAddSingleton<IMessageValidationPipeline>(sp => sp.GetRequiredService<MessageValidationPipeline>());

        return services;
    }

    /// <summary>
    /// Registers a message handler that is invoked after successful validation
    /// for messages of type <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The deserialized message type.</typeparam>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMessageHandler<TMessage, THandler>(
        this IServiceCollection services)
        where TMessage : class
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddScoped<IMessageHandler<TMessage>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a custom validation failure handler invoked when
    /// <see cref="FailureBehavior.Custom"/> is configured and a message fails validation.
    /// </summary>
    /// <typeparam name="THandler">The failure handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddValidationFailureHandler<THandler>(
        this IServiceCollection services)
        where THandler : class, IValidationFailureHandler
    {
        services.AddScoped<IValidationFailureHandler, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a dead-letter handler invoked when
    /// <see cref="FailureBehavior.DeadLetter"/> is configured and validation fails.
    /// </summary>
    public static IServiceCollection AddDeadLetterHandler<THandler>(
        this IServiceCollection services)
        where THandler : class, IDeadLetterHandler
    {
        services.AddScoped<IDeadLetterHandler, THandler>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="IMessageDeserializer"/> implementation used by the pipeline
    /// to convert raw payload bytes into typed message objects.
    /// </summary>
    /// <typeparam name="TDeserializer">The deserializer implementation type (e.g., a JSON or Protobuf deserializer).</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMessageDeserializer<TDeserializer>(
        this IServiceCollection services)
        where TDeserializer : class, IMessageDeserializer
    {
        services.AddSingleton<IMessageDeserializer, TDeserializer>();
        return services;
    }
}
