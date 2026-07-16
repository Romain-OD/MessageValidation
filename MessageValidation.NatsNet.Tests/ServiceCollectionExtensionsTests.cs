using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;

namespace MessageValidation.NatsNet.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNatsNetMessageValidation_RegistersNatsConnection()
    {
        var services = new ServiceCollection();

        services.AddNatsNetMessageValidation();

        var sp = services.BuildServiceProvider();
        var connection = sp.GetService<INatsConnection>();

        Assert.NotNull(connection);
    }

    [Fact]
    public void AddNatsNetMessageValidation_AppliesUrl()
    {
        var services = new ServiceCollection();

        services.AddNatsNetMessageValidation(url: "nats://example.com:4222");

        var sp = services.BuildServiceProvider();
        var connection = sp.GetRequiredService<INatsConnection>();

        Assert.Equal("nats://example.com:4222", connection.Opts.Url);
    }

    [Fact]
    public void AddNatsNetMessageValidation_InvokesConfigureOptionsCallback()
    {
        var callbackInvoked = false;

        var services = new ServiceCollection();

        services.AddNatsNetMessageValidation(configureOptions: opts =>
        {
            callbackInvoked = true;
            return opts with { Name = "test-connection" };
        });

        var sp = services.BuildServiceProvider();
        var connection = sp.GetRequiredService<INatsConnection>();

        Assert.True(callbackInvoked);
        Assert.Equal("test-connection", connection.Opts.Name);
    }
}
