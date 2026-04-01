using Microsoft.Extensions.DependencyInjection;
using MQTTnet;

namespace MessageValidation.MqttNet;

/// <summary>
/// Extension methods for registering MQTTnet integration with the MessageValidation pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a post-configuration action that automatically calls
    /// <see cref="MqttClientExtensions.UseMessageValidation"/> on the resolved
    /// <see cref="IMqttClient"/> when it is created from the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureMqttClient">
    /// Optional callback to further configure the <see cref="IMqttClient"/> after
    /// the validation pipeline is wired up.
    /// </param>
    public static IServiceCollection AddMqttNetMessageValidation(
        this IServiceCollection services,
        Action<IMqttClient>? configureMqttClient = null)
    {
        services.AddSingleton(sp =>
        {
            var factory = new MQTTnet.MqttClientFactory();
            var client = factory.CreateMqttClient();
            var pipeline = sp.GetRequiredService<IMessageValidationPipeline>();

            client.UseMessageValidation(pipeline);
            configureMqttClient?.Invoke(client);

            return client;
        });

        return services;
    }
}
