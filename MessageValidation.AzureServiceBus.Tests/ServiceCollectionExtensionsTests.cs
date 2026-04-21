using Azure.Core;
using Azure.Messaging.ServiceBus;
using MessageValidation.AzureServiceBus;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace MessageValidation.AzureServiceBus.Tests;

public class ServiceCollectionExtensionsTests
{
    private const string FakeConnectionString =
        "Endpoint=sb://fake.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=dGVzdGtleQ==";

    [Fact]
    public void AddAzureServiceBusMessageValidation_WithConnectionString_RegistersClient()
    {
        var services = new ServiceCollection();

        services.AddAzureServiceBusMessageValidation(FakeConnectionString);

        var sp = services.BuildServiceProvider();
        var client = sp.GetService<ServiceBusClient>();

        Assert.NotNull(client);
        Assert.Equal("fake.servicebus.windows.net", client!.FullyQualifiedNamespace);
    }

    [Fact]
    public void AddAzureServiceBusMessageValidation_InvokesConfigureOptionsCallback()
    {
        var callbackInvoked = false;

        var services = new ServiceCollection();

        services.AddAzureServiceBusMessageValidation(FakeConnectionString, options =>
        {
            callbackInvoked = true;
            options.RetryOptions.MaxRetries = 7;
        });

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<ServiceBusClient>();

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void AddAzureServiceBusMessageValidation_RegistersClientAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddAzureServiceBusMessageValidation(FakeConnectionString);

        var descriptor = Assert.Single(services.Where(d => d.ServiceType == typeof(ServiceBusClient)));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddAzureServiceBusMessageValidation_WithTokenCredential_RegistersClient()
    {
        var services = new ServiceCollection();
        var credential = Substitute.For<TokenCredential>();

        services.AddAzureServiceBusMessageValidation(
            fullyQualifiedNamespace: "contoso.servicebus.windows.net",
            credential: credential);

        var sp = services.BuildServiceProvider();
        var client = sp.GetService<ServiceBusClient>();

        Assert.NotNull(client);
        Assert.Equal("contoso.servicebus.windows.net", client!.FullyQualifiedNamespace);
    }

    [Fact]
    public void AddAzureServiceBusMessageValidation_NullConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddAzureServiceBusMessageValidation(connectionString: null!));
    }

    [Fact]
    public void AddAzureServiceBusMessageValidation_EmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddAzureServiceBusMessageValidation(connectionString: string.Empty));
    }

    [Fact]
    public void AddAzureServiceBusMessageValidation_NullCredential_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddAzureServiceBusMessageValidation(
                fullyQualifiedNamespace: "contoso.servicebus.windows.net",
                credential: null!));
    }
}
