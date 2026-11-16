using System.Collections.Concurrent;
using MessageValidation.SparkplugB.Protobuf;
using static MessageValidation.SparkplugB.SparkplugMessageType;
using ValueCase = MessageValidation.SparkplugB.Protobuf.Payload.Types.Metric.ValueOneofCase;

namespace MessageValidation.SparkplugB;

/// <summary>
/// Validates <see cref="SparkplugBMessage"/>s against the Sparkplug B 3.0 rules:
/// topic syntax, required payload fields per message type, metric values vs. declared
/// <see cref="DataType"/>, and — when session tracking is on — <c>seq</c> (0-255 rollover),
/// <c>bdSeq</c> and birth-declared metric types per edge node.
/// </summary>
/// <remarks>
/// Stateful: register as a singleton (done by
/// <see cref="SparkplugBServiceCollectionExtensions.AddSparkplugBValidation"/>).
/// With tracking on, data from an edge node is rejected until its NBIRTH is seen, as the
/// spec requires a host to request a rebirth in that case.
/// </remarks>
/// <param name="trackSessionState">
/// Track <c>seq</c>, <c>bdSeq</c> and birth metric types per edge node. Defaults to <see langword="true"/>.
/// </param>
public sealed class SparkplugBValidator(bool trackSessionState = true) : IMessageValidator<SparkplugBMessage>
{
    private const string BdSeq = "bdSeq";
    private const string Rebirth = "Node Control/Rebirth";

    // ponytail: in-memory, per-process state; scaled-out consumers sharing a node need shared state.
    private readonly ConcurrentDictionary<(string Group, string Edge), NodeState> _nodes = new();

    private sealed class NodeState
    {
        public bool Born;
        public ulong Seq;
        public ulong? BdSeq;
        public readonly Dictionary<(string Device, string Metric), DataType> Types = new();
    }

    /// <inheritdoc />
    public Task<MessageValidationResult> ValidateAsync(SparkplugBMessage message, CancellationToken ct = default) =>
        Task.FromResult(Validate(message));

    /// <summary>Parses and validates a raw MQTT topic and payload.</summary>
    public MessageValidationResult Validate(string topic, byte[] payload) =>
        Validate(SparkplugBMessage.Parse(topic, payload));

    /// <summary>Validates an already-parsed message.</summary>
    public MessageValidationResult Validate(SparkplugBMessage message)
    {
        if (message.ParseError is not null)
        {
            var part = message.Topic.MessageType == Unknown ? "topic" : "payload";
            return MessageValidationResult.Failure([new(part, message.ParseError)]);
        }

        var errors = new List<MessageValidationError>();
        if (!trackSessionState)
        {
            ValidateCore(message, null, errors);
        }
        else
        {
            var node = _nodes.GetOrAdd((message.Topic.GroupId, message.Topic.EdgeNodeId), _ => new NodeState());
            lock (node)
                ValidateCore(message, node, errors);
        }

        return errors.Count == 0 ? MessageValidationResult.Success() : MessageValidationResult.Failure(errors);
    }

    private static void ValidateCore(SparkplugBMessage message, NodeState? node, List<MessageValidationError> errors)
    {
        var (topic, p) = (message.Topic, message.Payload);
        var type = topic.MessageType;
        var isBirth = type is NBIRTH or DBIRTH;
        var device = topic.DeviceId ?? "";

        if (type != NDEATH && !p.HasTimestamp)
            errors.Add(new("timestamp", $"{type} requires a payload timestamp."));

        // seq: present on everything the edge node publishes except NDEATH (its MQTT Will).
        if (type is NBIRTH or DBIRTH or NDATA or DDATA or DDEATH)
        {
            if (!p.HasSeq)
                errors.Add(new("seq", $"{type} requires a seq number."));
            else if (p.Seq > 255)
                errors.Add(new("seq", $"seq must be between 0 and 255 but was {p.Seq}."));
        }
        else if (type == NDEATH && p.HasSeq)
        {
            errors.Add(new("seq", "NDEATH must not include a seq number."));
        }

        ulong? bdSeq = null;
        if (type is NBIRTH or NDEATH)
        {
            var m = p.Metrics.FirstOrDefault(m => m.Name == BdSeq);
            if (m is null || m.ValueCase != ValueCase.LongValue)
                errors.Add(new(BdSeq, $"{type} requires a '{BdSeq}' metric with a 64-bit integer value."));
            else
                bdSeq = m.LongValue;
        }

        if (type == NBIRTH && !p.Metrics.Any(m => m.Name == Rebirth && m.ValueCase == ValueCase.BooleanValue))
            errors.Add(new(Rebirth, $"NBIRTH requires a Boolean '{Rebirth}' metric."));

        if (node is not null)
            TrackSession(topic, p, bdSeq, node, errors);

        for (var i = 0; i < p.Metrics.Count; i++)
            ValidateMetric(p.Metrics[i], i, isBirth, device, node, errors);
    }

    private static void TrackSession(SparkplugTopic topic, Payload p, ulong? bdSeq, NodeState node, List<MessageValidationError> errors)
    {
        var edge = $"{topic.GroupId}/{topic.EdgeNodeId}";
        switch (topic.MessageType)
        {
            case NBIRTH:
                node.Born = true;
                node.Seq = p.Seq;
                node.BdSeq = bdSeq;
                node.Types.Clear();
                return;

            case NDEATH:
                if (node.Born && bdSeq is not null && bdSeq != node.BdSeq)
                {
                    errors.Add(new(BdSeq, $"Stale NDEATH for {edge}: bdSeq {bdSeq} does not match NBIRTH bdSeq {node.BdSeq}."));
                    return;
                }
                node.Born = false;
                node.Types.Clear();
                return;

            case DBIRTH or NDATA or DDATA or DDEATH:
                if (!node.Born)
                {
                    errors.Add(new("seq", $"No NBIRTH received for {edge}; a rebirth must be requested."));
                    return;
                }
                if (!p.HasSeq || p.Seq > 255)
                    return;
                var expected = (node.Seq + 1) % 256;
                if (p.Seq != expected)
                    errors.Add(new("seq", $"Out-of-order seq for {edge}: expected {expected} but was {p.Seq}."));
                // Resync on the received value so one lost message doesn't fail every later one.
                node.Seq = p.Seq;
                return;
        }
    }

    private static void ValidateMetric(
        Payload.Types.Metric m, int index, bool isBirth, string device, NodeState? node, List<MessageValidationError> errors)
    {
        var path = m.HasName ? $"metrics[{index}] ({m.Name})" : $"metrics[{index}]";

        if (isBirth)
        {
            if (string.IsNullOrEmpty(m.Name)) errors.Add(new(path, "Birth metrics require a name."));
            if (!m.HasDatatype) errors.Add(new(path, "Birth metrics require a datatype."));
        }
        else if (!m.HasName && !m.HasAlias)
        {
            errors.Add(new(path, "Metric requires a name or an alias."));
        }

        DataType? declared = m.HasDatatype ? (DataType)m.Datatype : null;
        if (declared is { } d && !IsMetricType(d))
        {
            errors.Add(new(path, $"Invalid metric datatype {m.Datatype}."));
            return;
        }

        if (node is { Born: true })
        {
            if (isBirth)
            {
                if (declared is { } bd)
                {
                    if (m.HasName) node.Types[(device, m.Name)] = bd;
                    if (m.HasAlias) node.Types[(device, "#" + m.Alias)] = bd;
                }
            }
            else
            {
                var key = m.HasAlias ? (device, "#" + m.Alias) : (device, m.Name);
                if (node.Types.TryGetValue(key, out var born))
                {
                    if (declared is { } dd && dd != born)
                        errors.Add(new(path, $"Datatype {dd} differs from {born} declared in the birth certificate."));
                    declared = born;
                }
                else if (m.HasName || m.HasAlias)
                {
                    errors.Add(new(path, "Metric was not declared in the birth certificate."));
                }
            }
        }

        if (m.IsNull)
        {
            if (m.ValueCase != ValueCase.None)
                errors.Add(new(path, "A metric with is_null=true must not carry a value."));
            return;
        }

        if (declared is not { } t || m.ValueCase == ValueCase.None)
            return;

        if (!Matches(t, m.ValueCase))
            errors.Add(new(path, $"Value {m.ValueCase} does not match declared datatype {t}."));
        else if (t == DataType.Uint8 && m.IntValue > byte.MaxValue || t == DataType.Uint16 && m.IntValue > ushort.MaxValue)
            errors.Add(new(path, $"Value {m.IntValue} is out of range for {t}."));
    }

    private static bool IsMetricType(DataType t) =>
        t is > DataType.Unknown and <= DataType.DateTimeArray and not DataType.PropertySet and not DataType.PropertySetList;

    // Sparkplug B 3.0 §6.4.16: value field per datatype. UInt32 accepts both encodings seen in the wild.
    private static bool Matches(DataType t, ValueCase c) => t switch
    {
        DataType.Int8 or DataType.Int16 or DataType.Int32 or DataType.Uint8 or DataType.Uint16 => c == ValueCase.IntValue,
        DataType.Uint32 => c is ValueCase.IntValue or ValueCase.LongValue,
        DataType.Int64 or DataType.Uint64 or DataType.DateTime => c == ValueCase.LongValue,
        DataType.Float => c == ValueCase.FloatValue,
        DataType.Double => c == ValueCase.DoubleValue,
        DataType.Boolean => c == ValueCase.BooleanValue,
        DataType.String or DataType.Text or DataType.Uuid => c == ValueCase.StringValue,
        DataType.DataSet => c == ValueCase.DatasetValue,
        DataType.Template => c == ValueCase.TemplateValue,
        _ => c == ValueCase.BytesValue, // Bytes, File and all array types
    };
}
