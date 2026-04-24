using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Net;

namespace MessageValidation.NatsNet;

/// <summary>
/// Extension methods for registering NATS.Net integration with the MessageValidation pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="INatsConnection"/> built from the specified NATS server URL.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="url">
    /// The NATS server URL (e.g., <c>nats://localhost:4222</c>). When null or empty, the
    /// default <see cref="NatsOpts"/> are used.
    /// </param>
    /// <param name="configureOptions">
    /// Optional callback that transforms the base <see cref="NatsOpts"/> (use the
    /// <c>with</c> expression to produce a new instance).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddNatsNetMessageValidation(
        this IServiceCollection services,
        string? url = null,
        Func<NatsOpts, NatsOpts>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<INatsConnection>(_ =>
        {
            var opts = NatsOpts.Default;
            if (!string.IsNullOrEmpty(url))
            {
                opts = opts with { Url = url };
            }

            if (configureOptions is not null)
            {
                opts = configureOptions(opts);
            }

            return new NatsClient(opts).Connection;
        });

        return services;
    }
}
