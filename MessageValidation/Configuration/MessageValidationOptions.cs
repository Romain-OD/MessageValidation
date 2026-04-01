using System.Collections.Concurrent;

namespace MessageValidation;

/// <summary>
/// Configuration options for the <see cref="IMessageValidationPipeline"/>.
/// Passed to <see cref="ServiceCollectionExtensions.AddMessageValidation"/> to
/// configure source-to-type mappings and failure handling behavior.
/// </summary>
public sealed class MessageValidationOptions
{
    private readonly ConcurrentDictionary<string, Type> _sourceMappings = new();

    /// <summary>
    /// Gets or sets the default behavior when a message fails validation.
    /// Defaults to <see cref="FailureBehavior.Log"/>.
    /// </summary>
    /// <seealso cref="FailureBehavior"/>
    public FailureBehavior DefaultFailureBehavior { get; set; } = FailureBehavior.Log;

    /// <summary>
    /// Gets or sets the prefix prepended to <see cref="MessageContext.Source"/>
    /// when building the <see cref="DeadLetterContext.Destination"/>.
    /// Defaults to <c>"$dead-letter/"</c>.
    /// </summary>
    public string? DeadLetterPrefix { get; set; } = "$dead-letter/";

    /// <summary>
    /// Maps a source pattern (topic, queue, routing key) to a message CLR type
    /// so the pipeline knows which type to deserialize and validate.
    /// Supports exact matches and MQTT-style wildcards (<c>+</c> single-level, <c>#</c> multi-level).
    /// </summary>
    /// <typeparam name="TMessage">The CLR type representing the message.</typeparam>
    /// <param name="sourcePattern">
    /// An exact source string or an MQTT-style wildcard pattern
    /// (e.g., <c>"sensors/+/temperature"</c> or <c>"orders/#"</c>).
    /// </param>
    /// <returns>This <see cref="MessageValidationOptions"/> instance for fluent chaining.</returns>
    public MessageValidationOptions MapSource<TMessage>(string sourcePattern)
        where TMessage : class
    {
        _sourceMappings[sourcePattern] = typeof(TMessage);
        return this;
    }

    /// <summary>
    /// Attempts to resolve the message CLR type for the given <paramref name="source"/>.
    /// Checks exact matches first, then falls back to MQTT-style wildcard patterns
    /// (<c>+</c> for single-level, <c>#</c> for multi-level).
    /// </summary>
    /// <param name="source">The actual source string from the incoming message.</param>
    /// <param name="messageType">When this method returns <see langword="true"/>, the resolved CLR type; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a mapping was found; otherwise <see langword="false"/>.</returns>
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
