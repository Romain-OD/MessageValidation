using System.Collections.Concurrent;

namespace MessageValidation;

/// <summary>
/// Configuration options for the message validation pipeline.
/// </summary>
public sealed class MessageValidationOptions
{
    private readonly ConcurrentDictionary<string, Type> _sourceMappings = new();

    /// <summary>
    /// Default behavior when validation fails.
    /// </summary>
    public FailureBehavior DefaultFailureBehavior { get; set; } = FailureBehavior.Log;

    /// <summary>
    /// Prefix for dead-letter destinations (topic, queue, etc.).
    /// </summary>
    public string? DeadLetterPrefix { get; set; } = "$dead-letter/";

    /// <summary>
    /// Maps a source pattern (topic, queue, routing key) to a message type.
    /// </summary>
    public MessageValidationOptions MapSource<TMessage>(string sourcePattern)
        where TMessage : class
    {
        _sourceMappings[sourcePattern] = typeof(TMessage);
        return this;
    }

    /// <summary>
    /// Attempts to resolve the message type for a given source.
    /// Supports exact match and simple MQTT-style wildcard patterns (+ and #).
    /// </summary>
    public bool TryResolveMessageType(string source, out Type? messageType)
    {
        // Exact match first
        if (_sourceMappings.TryGetValue(source, out messageType))
            return true;

        // Wildcard match
        foreach (var (pattern, type) in _sourceMappings)
        {
            if (MatchesPattern(source, pattern))
            {
                messageType = type;
                return true;
            }
        }

        messageType = null;
        return false;
    }

    private static bool MatchesPattern(string source, string pattern)
    {
        // Support MQTT-style wildcards: + (single level) and # (multi-level)
        var sourceSegments = source.Split('/');
        var patternSegments = pattern.Split('/');

        for (var i = 0; i < patternSegments.Length; i++)
        {
            if (patternSegments[i] == "#")
                return true; // # matches everything from this point

            if (i >= sourceSegments.Length)
                return false;

            if (patternSegments[i] != "+" && patternSegments[i] != sourceSegments[i])
                return false;
        }

        return sourceSegments.Length == patternSegments.Length;
    }
}
