using MQTTnet;
using MQTTnet.Client;

namespace MessageValidation.MqttNet;

/// <summary>
/// Extension methods for integrating the MessageValidation pipeline with an MQTTnet client.
/// </summary>
public static class MqttClientExtensions
{
    /// <summary>
    /// Hooks the <see cref="IMessageValidationPipeline"/> into the MQTTnet client's
    /// <see cref="IMqttClient.ApplicationMessageReceivedAsync"/> event so that every
    /// incoming message is automatically deserialized, validated, and dispatched.
    /// </summary>
    /// <param name="client">The MQTTnet client instance.</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <returns>The same <see cref="IMqttClient"/> for chaining.</returns>
    public static IMqttClient UseMessageValidation(
        this IMqttClient client,
        IMessageValidationPipeline pipeline)
    {
        client.ApplicationMessageReceivedAsync += async e =>
        {
            var context = new MessageContext
            {
                Source = e.ApplicationMessage.Topic,
                RawPayload = e.ApplicationMessage.PayloadSegment.ToArray(),
                Metadata = new Dictionary<string, object>
                {
                    ["mqtt.qos"] = e.ApplicationMessage.QualityOfServiceLevel,
                    ["mqtt.retain"] = e.ApplicationMessage.Retain,
                    ["mqtt.contentType"] = e.ApplicationMessage.ContentType ?? string.Empty,
                    ["mqtt.responseTopic"] = e.ApplicationMessage.ResponseTopic ?? string.Empty
                }
            };

            await pipeline.ProcessAsync(context);
        };

        return client;
    }
}
