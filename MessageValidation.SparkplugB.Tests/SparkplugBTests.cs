using Google.Protobuf;
using MessageValidation.SparkplugB.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Metric = MessageValidation.SparkplugB.Protobuf.Payload.Types.Metric;

namespace MessageValidation.SparkplugB.Tests;

public class SparkplugBTests
{
    private const string Node = "spBv1.0/Plant1/{0}/Edge1";

    private static string T(string type, string? device = null) =>
        string.Format(Node, type) + (device is null ? "" : "/" + device);

    private static Metric M(string name, DataType type, Action<Metric> value)
    {
        var m = new Metric { Name = name, Datatype = (uint)type };
        value(m);
        return m;
    }

    private static byte[] NBirth(ulong seq = 0, ulong bdSeq = 7) => new Payload
    {
        Timestamp = 1, Seq = seq,
        Metrics =
        {
            M("bdSeq", DataType.Int64, m => m.LongValue = bdSeq),
            M("Node Control/Rebirth", DataType.Boolean, m => m.BooleanValue = false),
            M("Temperature", DataType.Double, m => { m.Alias = 1; m.DoubleValue = 21.5; }),
        },
    }.ToByteArray();

    private static byte[] NData(ulong seq, Action<Metric>? value = null)
    {
        var m = new Metric { Alias = 1 };
        (value ?? (x => x.DoubleValue = 22))(m);
        return new Payload { Timestamp = 2, Seq = seq, Metrics = { m } }.ToByteArray();
    }

    private static string[] Errors(MessageValidationResult r) => r.Errors.Select(e => e.ErrorMessage).ToArray();

    [Theory]
    [InlineData("spBv1.0/G/NBIRTH/E", null)]
    [InlineData("spBv1.0/G/DDATA/E/D", null)]
    [InlineData("spBv1.0/G/NDATA/E/D", "must not have a device_id")]
    [InlineData("spBv1.0/G/DDATA/E", "require a device_id")]
    [InlineData("spBv1.0/G/FOO/E", "Unknown Sparkplug message type")]
    [InlineData("spAv1.0/G/NDATA/E", "must match")]
    [InlineData("spBv1.0/G+/NDATA/E", "group_id")]
    [InlineData("spBv1.0/G/NDATA/", "edge_node_id")]
    [InlineData("spBv1.0/STATE/host", "must match")]
    public void Topic_IsParsed(string topic, string? expectedError)
    {
        var ok = SparkplugTopic.TryParse(topic, out _, out var error);

        Assert.Equal(expectedError is null, ok);
        if (expectedError is not null) Assert.Contains(expectedError, error);
    }

    [Fact]
    public void ValidBirthAndData_Pass()
    {
        var v = new SparkplugBValidator();

        Assert.True(v.Validate(T("NBIRTH"), NBirth()).IsValid);
        Assert.True(v.Validate(T("NDATA"), NData(1)).IsValid);
    }

    [Fact]
    public void Garbage_Payload_Fails()
    {
        var r = new SparkplugBValidator().Validate(T("NDATA"), [0xFF, 0xFF, 0xFF]);

        var error = Assert.Single(r.Errors);
        Assert.Equal("payload", error.PropertyName);
        Assert.Contains("not a valid Sparkplug B protobuf", error.ErrorMessage);
    }

    [Fact]
    public void NBirth_WithoutBdSeqOrTimestamp_Fails()
    {
        var payload = new Payload { Seq = 0 }.ToByteArray();

        var r = new SparkplugBValidator().Validate(T("NBIRTH"), payload);

        Assert.Contains(r.Errors, e => e.PropertyName == "bdSeq");
        Assert.Contains(r.Errors, e => e.PropertyName == "timestamp");
        Assert.Contains(r.Errors, e => e.PropertyName == "Node Control/Rebirth");
    }

    [Fact]
    public void ValueNotMatchingDeclaredType_Fails()
    {
        var v = new SparkplugBValidator(trackSessionState: false);
        var payload = new Payload
        {
            Timestamp = 1, Seq = 1,
            Metrics = { M("Speed", DataType.Float, m => m.StringValue = "fast") },
        }.ToByteArray();

        var r = v.Validate(T("NDATA"), payload);

        Assert.Contains("does not match declared datatype Float", Assert.Single(Errors(r)));
    }

    [Fact]
    public void DataByAlias_IsCheckedAgainstBirthDatatype()
    {
        var v = new SparkplugBValidator();
        v.Validate(T("NBIRTH"), NBirth());

        var r = v.Validate(T("NDATA"), NData(1, m => m.BooleanValue = true));

        Assert.Contains("does not match declared datatype Double", Assert.Single(Errors(r)));
    }

    [Fact]
    public void Seq_RollsOverFrom255To0()
    {
        var v = new SparkplugBValidator();
        v.Validate(T("NBIRTH"), NBirth(seq: 254));

        Assert.True(v.Validate(T("NDATA"), NData(255)).IsValid);
        Assert.True(v.Validate(T("NDATA"), NData(0)).IsValid);
    }

    [Fact]
    public void Seq_Gap_FailsThenResyncs()
    {
        var v = new SparkplugBValidator();
        v.Validate(T("NBIRTH"), NBirth());

        Assert.Contains("expected 1 but was 3", Assert.Single(Errors(v.Validate(T("NDATA"), NData(3)))));
        Assert.True(v.Validate(T("NDATA"), NData(4)).IsValid);
    }

    [Fact]
    public void Data_BeforeBirth_Fails()
    {
        var r = new SparkplugBValidator().Validate(T("NDATA"), NData(1));

        Assert.Contains("No NBIRTH", Assert.Single(Errors(r)));
    }

    [Fact]
    public void Ndeath_WithStaleBdSeq_Fails()
    {
        var v = new SparkplugBValidator();
        v.Validate(T("NBIRTH"), NBirth(bdSeq: 7));
        var death = new Payload { Metrics = { M("bdSeq", DataType.Int64, m => m.LongValue = 6) } }.ToByteArray();

        Assert.Contains("Stale NDEATH", Assert.Single(Errors(v.Validate(T("NDEATH"), death))));
    }

    [Fact]
    public async Task Pipeline_DispatchesValidMessages_AndBlocksInvalidOnes()
    {
        var handled = new List<SparkplugBMessage>();
        var services = new ServiceCollection()
            .AddMessageValidation(
                o => o.MapSource<SparkplugBMessage>("spBv1.0/#"),
                b => { b.UseSparkplugB(); MessageValidationPipeline.ConfigureDefaults(b); })
            .AddSparkplugBValidation()
            .AddSingleton<IMessageHandler<SparkplugBMessage>>(new Handler(handled));
        await using var sp = services.BuildServiceProvider();
        var pipeline = sp.GetRequiredService<IMessageValidationPipeline>();

        await pipeline.ProcessAsync(new MessageContext { Source = T("NBIRTH"), RawPayload = NBirth() });
        await pipeline.ProcessAsync(new MessageContext { Source = T("NDATA"), RawPayload = NData(5) });

        var msg = Assert.Single(handled);
        Assert.Equal(SparkplugMessageType.NBIRTH, msg.Topic.MessageType);
        Assert.Equal("Edge1", msg.Topic.EdgeNodeId);
    }

    private sealed class Handler(List<SparkplugBMessage> handled) : IMessageHandler<SparkplugBMessage>
    {
        public Task HandleAsync(SparkplugBMessage message, MessageContext context, CancellationToken ct = default)
        {
            handled.Add(message);
            return Task.CompletedTask;
        }
    }
}
