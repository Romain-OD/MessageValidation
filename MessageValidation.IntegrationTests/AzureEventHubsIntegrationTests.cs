using System.Net;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using MessageValidation.AzureEventHubs;
using Testcontainers.Azurite;

namespace MessageValidation.IntegrationTests;

public class AzureEventHubsIntegrationTests
{
    private const string ConsumerGroup = "integration-group";
    private const string EventHubName = "integration-hub";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Consumer_ReceivesEventThroughValidationPipeline()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = timeout.Token;
        var configPath = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure",
            "EventHubs",
            "Config.json");

        await using var network = new NetworkBuilder().Build();
        await using var azurite = new AzuriteBuilder(
                "mcr.microsoft.com/azure-storage/azurite:3.36.0")
            .WithNetwork(network)
            .WithNetworkAliases("azurite")
            .Build();
        await using var emulator = new ContainerBuilder(
                "mcr.microsoft.com/azure-messaging/eventhubs-emulator:2.2.1")
            .WithBindMount(
                configPath,
                "/Eventhubs_Emulator/ConfigFiles/Config.json",
                AccessMode.ReadOnly)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("BLOB_SERVER", "azurite")
            .WithEnvironment("METADATA_SERVER", "azurite")
            .WithNetwork(network)
            .WithNetworkAliases("eventhubs-emulator")
            .WithPortBinding(5672, true)
            .WithPortBinding(9092, true)
            .WithPortBinding(5300, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(5300)
                    .ForPath("/health")
                    .ForStatusCode(HttpStatusCode.OK)))
            .Build();

        await network.CreateAsync(ct);
        await azurite.StartAsync(ct);
        await emulator.StartAsync(ct);

        var connectionString =
            $"Endpoint=sb://{emulator.Hostname}:{emulator.GetMappedPublicPort(5672)};" +
            "SharedAccessKeyName=RootManageSharedAccessKey;" +
            "SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

        await using var producer = new EventHubProducerClient(connectionString, EventHubName);
        await using var consumer = new EventHubConsumerClient(
            ConsumerGroup,
            connectionString,
            EventHubName);
        var pipeline = new RecordingPipeline();
        using var consumingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var consumingTask = consumer.StartConsuming(
            pipeline,
            startReadingAtEarliestEvent: true,
            ct: consumingCts.Token);

        var payload = """{"temperature":21.5}"""u8.ToArray();
        MessageContext context;

        try
        {
            var eventData = new EventData(new BinaryData(payload))
            {
                MessageId = "eventhubs-integration-1",
                CorrelationId = "correlation-1",
                ContentType = "application/json"
            };
            eventData.Properties["test"] = "integration";

            await producer.SendAsync([eventData], ct);
            context = await pipeline.WaitAsync(ct);
        }
        finally
        {
            await consumingCts.CancelAsync();
            await consumingTask.WaitAsync(TimeSpan.FromSeconds(30));
        }

        Assert.Equal(payload, context.RawPayload);
        Assert.Equal(EventHubName, context.Source);
        Assert.Equal(EventHubName, context.Metadata["eventhubs.eventHubName"]);
        Assert.Equal("eventhubs-integration-1", context.Metadata["eventhubs.messageId"]);
        Assert.Equal("application/json", context.Metadata["eventhubs.contentType"]);
        Assert.NotEmpty(Assert.IsType<string>(context.Metadata["eventhubs.partitionId"]));
        var properties = Assert.IsAssignableFrom<IDictionary<string, object>>(
            context.Metadata["eventhubs.properties"]);
        Assert.Equal("integration", properties["test"]);
    }
}
