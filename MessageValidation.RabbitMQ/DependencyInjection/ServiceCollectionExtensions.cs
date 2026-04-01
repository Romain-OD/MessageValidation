using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace MessageValidation.RabbitMQ;

/// <summary>
/// Extension methods for registering RabbitMQ integration with the MessageValidation pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="IConnection"/> created from the supplied
    /// <see cref="ConnectionFactory"/> and provides a factory method for creating
    /// pipeline-wired channels via <see cref="IChannel"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureFactory">Callback to configure the <see cref="ConnectionFactory"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddRabbitMqMessageValidation(
        this IServiceCollection services,
        Action<ConnectionFactory> configureFactory)
    {
        services.AddSingleton(sp =>
        {
            var factory = new ConnectionFactory();
            configureFactory(factory);
            return factory;
        });

        services.AddSingleton<IConnection>(sp =>
        {
            var factory = sp.GetRequiredService<ConnectionFactory>();
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        return services;
    }
}
