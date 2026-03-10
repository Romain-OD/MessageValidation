using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MessageValidation;

/// <summary>
/// Extension methods for registering MessageValidation services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core message validation pipeline and configures source-to-type mappings.
    /// </summary>
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
        services.AddSingleton<MessageValidationPipeline>();
        services.TryAddSingleton<IMessageValidationPipeline>(sp => sp.GetRequiredService<MessageValidationPipeline>());

        return services;
    }

    /// <summary>
    /// Registers a message handler for a specific message type.
    /// </summary>
    public static IServiceCollection AddMessageHandler<TMessage, THandler>(
        this IServiceCollection services)
        where TMessage : class
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddScoped<IMessageHandler<TMessage>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a custom validation failure handler.
    /// </summary>
    public static IServiceCollection AddValidationFailureHandler<THandler>(
        this IServiceCollection services)
        where THandler : class, IValidationFailureHandler
    {
        services.AddScoped<IValidationFailureHandler, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a custom message deserializer.
    /// </summary>
    public static IServiceCollection AddMessageDeserializer<TDeserializer>(
        this IServiceCollection services)
        where TDeserializer : class, IMessageDeserializer
    {
        services.AddSingleton<IMessageDeserializer, TDeserializer>();
        return services;
    }
}
