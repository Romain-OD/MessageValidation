using Azure.Core;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.AzureServiceBus;

/// <summary>
/// Extension methods for registering Azure Service Bus integration with the MessageValidation pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="ServiceBusClient"/> built from the specified
    /// connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">
    /// A Service Bus namespace connection string
    /// (e.g., <c>Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...</c>).
    /// </param>
    /// <param name="configureOptions">Optional callback to configure <see cref="ServiceBusClientOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddAzureServiceBusMessageValidation(
        this IServiceCollection services,
        string connectionString,
        Action<ServiceBusClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        services.AddSingleton(_ =>
        {
            var options = new ServiceBusClientOptions();
            configureOptions?.Invoke(options);
            return new ServiceBusClient(connectionString, options);
        });

        return services;
    }

    /// <summary>
    /// Registers a singleton <see cref="ServiceBusClient"/> built from the specified
    /// fully qualified namespace and <see cref="TokenCredential"/>
    /// (e.g., <c>DefaultAzureCredential</c>, <c>ManagedIdentityCredential</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="fullyQualifiedNamespace">
    /// The fully qualified Service Bus namespace (e.g., <c>contoso.servicebus.windows.net</c>).
    /// </param>
    /// <param name="credential">A <see cref="TokenCredential"/> used to authenticate.</param>
    /// <param name="configureOptions">Optional callback to configure <see cref="ServiceBusClientOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddAzureServiceBusMessageValidation(
        this IServiceCollection services,
        string fullyQualifiedNamespace,
        TokenCredential credential,
        Action<ServiceBusClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(fullyQualifiedNamespace);
        ArgumentNullException.ThrowIfNull(credential);

        services.AddSingleton(_ =>
        {
            var options = new ServiceBusClientOptions();
            configureOptions?.Invoke(options);
            return new ServiceBusClient(fullyQualifiedNamespace, credential, options);
        });

        return services;
    }
}
