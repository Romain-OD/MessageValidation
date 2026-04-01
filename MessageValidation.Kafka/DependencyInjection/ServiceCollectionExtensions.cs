using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.Kafka;

/// <summary>
/// Extension methods for registering Confluent Kafka integration with the MessageValidation pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ConsumerConfig"/> and a singleton <see cref="IConsumer{TKey,TValue}"/>
    /// (keyed <c>string</c>, value <c>byte[]</c>) built from the supplied configuration callback.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureConsumer">
    /// Callback to configure the <see cref="ConsumerConfig"/>.
    /// At minimum set <see cref="ConsumerConfig.BootstrapServers"/> and
    /// <see cref="ConsumerConfig.GroupId"/>.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddKafkaMessageValidation(
        this IServiceCollection services,
        Action<ConsumerConfig> configureConsumer)
    {
        services.AddSingleton(sp =>
        {
            var config = new ConsumerConfig();
            configureConsumer(config);
            return config;
        });

        services.AddSingleton<IConsumer<string, byte[]>>(sp =>
        {
            var config = sp.GetRequiredService<ConsumerConfig>();
            return new ConsumerBuilder<string, byte[]>(config).Build();
        });

        return services;
    }
}
