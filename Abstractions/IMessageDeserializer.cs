namespace MessageValidation;

/// <summary>
/// Defines a deserializer that converts raw message bytes into a typed object.
/// </summary>
public interface IMessageDeserializer
{
    object Deserialize(byte[] payload, Type targetType);
}
