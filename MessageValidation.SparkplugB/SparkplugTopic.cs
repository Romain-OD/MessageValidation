using System.Diagnostics.CodeAnalysis;

namespace MessageValidation.SparkplugB;

/// <summary>Sparkplug B message types carried in the third topic segment.</summary>
public enum SparkplugMessageType
{
    /// <summary>Unrecognised or unparsed message type.</summary>
    Unknown,
    /// <summary>Edge node birth certificate.</summary>
    NBIRTH,
    /// <summary>Edge node death certificate (usually the MQTT Last Will).</summary>
    NDEATH,
    /// <summary>Device birth certificate.</summary>
    DBIRTH,
    /// <summary>Device death certificate.</summary>
    DDEATH,
    /// <summary>Edge node data.</summary>
    NDATA,
    /// <summary>Device data.</summary>
    DDATA,
    /// <summary>Command sent to an edge node.</summary>
    NCMD,
    /// <summary>Command sent to a device.</summary>
    DCMD,
}

/// <summary>
/// A parsed Sparkplug B topic: <c>spBv1.0/&lt;group_id&gt;/&lt;message_type&gt;/&lt;edge_node_id&gt;[/&lt;device_id&gt;]</c>.
/// </summary>
public sealed record SparkplugTopic(
    string GroupId,
    SparkplugMessageType MessageType,
    string EdgeNodeId,
    string? DeviceId = null)
{
    /// <summary>The Sparkplug B topic namespace.</summary>
    public const string Namespace = "spBv1.0";

    /// <summary><see langword="true"/> for DBIRTH, DDEATH, DDATA and DCMD.</summary>
    public bool IsDeviceMessage => IsDeviceType(MessageType);

    /// <summary>Parses and validates a Sparkplug B topic string.</summary>
    public static bool TryParse(
        string topic,
        [NotNullWhen(true)] out SparkplugTopic? result,
        [NotNullWhen(false)] out string? error)
    {
        result = null;
        var s = topic.Split('/');

        if (s.Length is < 4 or > 5 || s[0] != Namespace)
        {
            error = $"Topic '{topic}' must match '{Namespace}/<group_id>/<message_type>/<edge_node_id>[/<device_id>]'.";
            return false;
        }

        var type = s[2] switch
        {
            "NBIRTH" => SparkplugMessageType.NBIRTH,
            "NDEATH" => SparkplugMessageType.NDEATH,
            "DBIRTH" => SparkplugMessageType.DBIRTH,
            "DDEATH" => SparkplugMessageType.DDEATH,
            "NDATA" => SparkplugMessageType.NDATA,
            "DDATA" => SparkplugMessageType.DDATA,
            "NCMD" => SparkplugMessageType.NCMD,
            "DCMD" => SparkplugMessageType.DCMD,
            _ => SparkplugMessageType.Unknown,
        };

        if (type == SparkplugMessageType.Unknown)
        {
            error = $"Unknown Sparkplug message type '{s[2]}'.";
            return false;
        }

        if (IsDeviceType(type) != (s.Length == 5))
        {
            error = IsDeviceType(type)
                ? $"{type} topics require a device_id segment."
                : $"{type} topics must not have a device_id segment.";
            return false;
        }

        string[] names = ["", "group_id", "", "edge_node_id", "device_id"];
        for (var i = 1; i < s.Length; i++)
        {
            if (i == 2) continue;
            if (s[i].Length == 0 || s[i].AsSpan().IndexOfAny('+', '#') >= 0)
            {
                error = $"{names[i]} '{s[i]}' must be non-empty and must not contain '+' or '#'.";
                return false;
            }
        }

        result = new SparkplugTopic(s[1], type, s[3], s.Length == 5 ? s[4] : null);
        error = null;
        return true;
    }

    private static bool IsDeviceType(SparkplugMessageType t) =>
        t is SparkplugMessageType.DBIRTH or SparkplugMessageType.DDEATH
          or SparkplugMessageType.DDATA or SparkplugMessageType.DCMD;
}
