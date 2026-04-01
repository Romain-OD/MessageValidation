namespace MessageValidation;

/// <summary>
/// Defines a deserializer that converts raw message bytes into a typed object.
/// The pipeline calls this before validation and handling.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to plug in your preferred serialization format
/// (JSON, Protobuf, Avro, MessagePack, etc.).
/// Register with:
/// <c>services.AddMessageDeserializer&lt;MyDeserializer&gt;()</c>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class JsonMessageDeserializer : IMessageDeserializer
/// {
///     public object Deserialize(byte[] payload, Type targetType)
///         =&gt; JsonSerializer.Deserialize(payload, targetType)
///             ?? throw new InvalidOperationException("Deserialization returned null.");
/// }
/// </code>
/// </example>
public interface IMessageDeserializer
{
    /// <summary>
    /// Deserializes the raw <paramref name="payload"/> into an instance of <paramref name="targetType"/>.
    /// </summary>
    /// <param name="payload">The raw message bytes received from the transport.</param>
    /// <param name="targetType">
    /// The CLR type resolved by <see cref="MessageValidationOptions.TryResolveMessageType"/>
    /// from the message source.
    /// </param>
    /// <returns>The deserialized object, which will be passed to the validator and handler.</returns>
    object Deserialize(byte[] payload, Type targetType);
}
