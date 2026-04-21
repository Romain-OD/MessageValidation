using Azure.Core;
using Azure.Messaging.EventHubs.Consumer;
using MessageValidation.AzureEventHubs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace MessageValidation.AzureEventHubs.Tests;

public class ServiceCollectionExtensionsTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=dGVzdGtleQ==;EntityPath=telemetry";

    [Fact]
    public void AddAzureEventHubsMessageValidation_WithConnectionString_RegistersConsumerClient()
    {
        var services = new ServiceCollection();

        services.AddAzureEventHubsMessageValidation(FakeConnectionString, eventHubName: "telemetry");

        var sp = services.BuildServiceProvider();
        var client = sp.GetService<EventHubConsumerClient>();

        Assert.NotNull(client);
        Assert.Equal("telemetry", client!.EventHubName);
        Assert.Equal(EventHubConsumerClient.DefaultConsumerGroupName, client.ConsumerGroup);
    }

    [Fact]
    public void AddAzureEventHubsMessageValidation_UsesCustomConsumerGroup()
    {
        var services = new ServiceCollection();

        services.AddAzureEventHubsMessageValidation(
            FakeConnectionString,
            eventHubName: "telemetry",
            consumerGroup: "analytics");

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<EventHubConsumerClient>();

        Assert.Equal("analytics", client.ConsumerGroup);
    }

    [Fact]
    public void AddAzureEventHubsMessageValidation_RegistersAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddAzureEventHubsMessageValidation(FakeConnectionString, eventHubName: "telemetry");

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(EventHubConsumerClient));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddAzureEventHubsMessageValidation_WithTokenCredential_RegistersConsumerClient()
    {
        var services = new ServiceCollection();
        var credential = Substitute.For<TokenCredential>();

        services.AddAzureEventHubsMessageValidation(
            fullyQualifiedNamespace: "contoso.servicebus.windows.net",
            eventHubName: "telemetry",
            credential: credential);

        var sp = services.BuildServiceProvider();
        var client = sp.GetService<EventHubConsumerClient>();

        Assert.NotNull(client);
        Assert.Equal("telemetry", client!.EventHubName);
        Assert.Equal("contoso.servicebus.windows.net", client.FullyQualifiedNamespace);
    }

    [Fact]
    public void AddAzureEventHubsMessageValidation_NullConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddAzureEventHubsMessageValidation(connectionString: null!, eventHubName: "telemetry"));
    }

    [Fact]
    public void AddAzureEventHubsMessageValidation_EmptyEventHubName_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddAzureEventHubsMessageValidation(FakeConnectionString, eventHubName: string.Empty));
    }

    [Fact]
    public void AddAzureEventHubsMessageValidation_NullCredential_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddAzureEventHubsMessageValidation(
                fullyQualifiedNamespace: "contoso.servicebus.windows.net",
                eventHubName: "telemetry",
                credential: null!));
    }
}
