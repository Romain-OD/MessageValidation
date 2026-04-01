using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace MessageValidation.RabbitMQ.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRabbitMqMessageValidation_RegistersConnectionFactory()
    {
        var services = new ServiceCollection();

        services.AddRabbitMqMessageValidation(factory =>
        {
            factory.HostName = "test-host";
        });

        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<ConnectionFactory>();

        Assert.NotNull(factory);
        Assert.Equal("test-host", factory.HostName);
    }

    [Fact]
    public void AddRabbitMqMessageValidation_InvokesConfigureCallback()
    {
        var callbackInvoked = false;

        var services = new ServiceCollection();

        services.AddRabbitMqMessageValidation(factory =>
        {
            callbackInvoked = true;
            factory.HostName = "localhost";
        });

        var sp = services.BuildServiceProvider();
        _ = sp.GetRequiredService<ConnectionFactory>();

        Assert.True(callbackInvoked);
    }
}
