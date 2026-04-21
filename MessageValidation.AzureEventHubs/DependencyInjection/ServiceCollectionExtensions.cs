using Azure.Core;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.AzureEventHubs;

/// <summary>
/// Extension methods for registering Azure Event Hubs integration with the MessageValidation pipeline.
/// </summary>
/// <remarks>
/// <para>
/// These helpers register an <see cref="EventHubConsumerClient"/> — the lightweight,
/// single-process consumer. For production workloads that need partition ownership
/// and checkpointing, build an <c>EventProcessorClient</c> explicitly and call
/// <see cref="EventProcessorClientExtensions.UseMessageValidation"/> on it.
/// </para>
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="EventHubConsumerClient"/> built from the
    /// specified connection string and event hub name.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">
    /// An Event Hubs namespace or entity connection string.
    /// </param>
    /// <param name="eventHubName">The event hub (entity) name to consume from.</param>
    /// <param name="consumerGroup">
    /// Optional consumer group. Defaults to <see cref="EventHubConsumerClient.DefaultConsumerGroupName"/> (<c>$Default</c>).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddAzureEventHubsMessageValidation(
        this IServiceCollection services,
        string connectionString,
        string eventHubName,
        string? consumerGroup = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);
        ArgumentException.ThrowIfNullOrEmpty(eventHubName);

        var group = string.IsNullOrEmpty(consumerGroup)
            ? EventHubConsumerClient.DefaultConsumerGroupName
            : consumerGroup;

        services.AddSingleton(_ => new EventHubConsumerClient(group, connectionString, eventHubName));

        return services;
    }

    /// <summary>
    /// Registers a singleton <see cref="EventHubConsumerClient"/> built from the
    /// specified fully qualified namespace, event hub name, and <see cref="TokenCredential"/>
    /// (e.g., <c>DefaultAzureCredential</c>, <c>ManagedIdentityCredential</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="fullyQualifiedNamespace">
    /// The fully qualified Event Hubs namespace (e.g., <c>contoso.servicebus.windows.net</c>).
    /// </param>
    /// <param name="eventHubName">The event hub (entity) name to consume from.</param>
    /// <param name="credential">A <see cref="TokenCredential"/> used to authenticate.</param>
    /// <param name="consumerGroup">
    /// Optional consumer group. Defaults to <see cref="EventHubConsumerClient.DefaultConsumerGroupName"/> (<c>$Default</c>).
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddAzureEventHubsMessageValidation(
        this IServiceCollection services,
        string fullyQualifiedNamespace,
        string eventHubName,
        TokenCredential credential,
        string? consumerGroup = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(fullyQualifiedNamespace);
        ArgumentException.ThrowIfNullOrEmpty(eventHubName);
        ArgumentNullException.ThrowIfNull(credential);

        var group = string.IsNullOrEmpty(consumerGroup)
            ? EventHubConsumerClient.DefaultConsumerGroupName
            : consumerGroup;

        services.AddSingleton(_ => new EventHubConsumerClient(
            group,
            fullyQualifiedNamespace,
            eventHubName,
            credential));

        return services;
    }
}
