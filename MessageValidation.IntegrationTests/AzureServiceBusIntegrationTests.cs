using System.Net;
using Azure.Messaging.ServiceBus;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using MessageValidation.AzureServiceBus;
using Testcontainers.MsSql;

namespace MessageValidation.IntegrationTests;

public class AzureServiceBusIntegrationTests
{
    private const string QueueName = "integration-queue";
    private const string SqlPassword = "MessageValidation1!";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Processor_ReceivesMessageThroughValidationPipeline()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = timeout.Token;
        var configPath = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure",
            "ServiceBus",
            "Config.json");

        await using var network = new NetworkBuilder().Build();
        await using var sql = new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2022-CU26-ubuntu-22.04")
            .WithPassword(SqlPassword)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithNetwork(network)
            .WithNetworkAliases("mssql")
            .Build();
        await using var emulator = new ContainerBuilder(
                "mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.1")
            .WithBindMount(
                configPath,
                "/ServiceBus_Emulator/ConfigFiles/Config.json",
                AccessMode.ReadOnly)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SQL_SERVER", "mssql")
            .WithEnvironment("MSSQL_SA_PASSWORD", SqlPassword)
            .WithEnvironment("EMULATOR_HTTP_PORT", "5300")
            .WithNetwork(network)
            .WithNetworkAliases("servicebus-emulator")
            .WithPortBinding(5672, true)
            .WithPortBinding(5300, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(5300)
                    .ForPath("/health")
                    .ForStatusCode(HttpStatusCode.OK)))
            .Build();

        await network.CreateAsync(ct);
        await sql.StartAsync(ct);
        await emulator.StartAsync(ct);

        var connectionString =
            $"Endpoint=sb://{emulator.Hostname}:{emulator.GetMappedPublicPort(5672)};" +
            "SharedAccessKeyName=RootManageSharedAccessKey;" +
            "SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        await using var client = new ServiceBusClient(connectionString);
        await using var sender = client.CreateSender(QueueName);
        await using var processor = client.CreateProcessor(
            QueueName,
            new ServiceBusProcessorOptions { MaxConcurrentCalls = 1 });
        var pipeline = new RecordingPipeline();
        processor.UseMessageValidation(pipeline);

        var payload = """{"orderId":42}"""u8.ToArray();
        MessageContext context;

        await processor.StartProcessingAsync(ct);
        try
        {
            var message = new ServiceBusMessage(new BinaryData(payload))
            {
                MessageId = "servicebus-integration-1",
                Subject = "order.created",
                CorrelationId = "correlation-1",
                ContentType = "application/json"
            };
            message.ApplicationProperties["test"] = "integration";

            await sender.SendMessageAsync(message, ct);
            context = await pipeline.WaitAsync(ct);
        }
        finally
        {
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await processor.StopProcessingAsync(stopTimeout.Token);
        }

        Assert.Equal(payload, context.RawPayload);
        Assert.Equal("order.created", context.Source);
        Assert.Equal(QueueName, context.Metadata["servicebus.entityPath"]);
        Assert.Equal("servicebus-integration-1", context.Metadata["servicebus.messageId"]);
        Assert.Equal("application/json", context.Metadata["servicebus.contentType"]);
        var properties = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            context.Metadata["servicebus.applicationProperties"]);
        Assert.Equal("integration", properties["test"]);
    }
}
