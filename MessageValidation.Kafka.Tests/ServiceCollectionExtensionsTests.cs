using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.Kafka.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddKafkaMessageValidation_RegistersConsumerConfig()
    {
        var services = new ServiceCollection();

        services.AddKafkaMessageValidation(config =>
        {
            config.BootstrapServers = "localhost:9092";
            config.GroupId = "test-group";
        });

        var sp = services.BuildServiceProvider();
        var config = sp.GetService<ConsumerConfig>();

        Assert.NotNull(config);
        Assert.Equal("localhost:9092", config.BootstrapServers);
        Assert.Equal("test-group", config.GroupId);
    }

    [Fact]
    public void AddKafkaMessageValidation_InvokesConfigureCallback()
    {
        var callbackInvoked = false;

        var services = new ServiceCollection();

        services.AddKafkaMessageValidation(config =>
        {
            callbackInvoked = true;
            config.BootstrapServers = "localhost:9092";
            config.GroupId = "test-group";
        });

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<ConsumerConfig>();

        Assert.True(callbackInvoked);
    }

    [Fact]
    public void AddKafkaMessageValidation_RegistersIConsumer()
    {
        var services = new ServiceCollection();

        services.AddKafkaMessageValidation(config =>
        {
            config.BootstrapServers = "localhost:9092";
            config.GroupId = "test-group";
        });

        var sp = services.BuildServiceProvider();

        // Verify the registration exists without resolving (avoids connecting to Kafka)
        var descriptor = Assert.Single(
            sp.GetService<IServiceProvider>() is not null
                ? services.Where(d => d.ServiceType == typeof(IConsumer<string, byte[]>))
                : []);

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}
