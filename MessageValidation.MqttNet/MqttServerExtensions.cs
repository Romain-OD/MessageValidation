using MQTTnet;
using MQTTnet.Server;

namespace MessageValidation.MqttNet;

/// <summary>
/// Extension methods for integrating the MessageValidation pipeline with an MQTTnet server.
/// </summary>
public static class MqttServerExtensions
{
    /// <summary>
    /// Hooks the <see cref="MessageValidationPipeline"/> into the MQTTnet server's
    /// <see cref="MqttServer.InterceptingPublishAsync"/> event so that every
    /// published message is automatically deserialized, validated, and dispatched.
    /// </summary>
    /// <param name="server">The MQTTnet server instance.</param>
    /// <param name="pipeline">The MessageValidation pipeline.</param>
    /// <returns>The same <see cref="MqttServer"/> for chaining.</returns>
    public static MqttServer UseMessageValidation(
        this MqttServer server,
        MessageValidationPipeline pipeline)
    {
        server.InterceptingPublishAsync += async e =>
        {
            var context = new MessageContext
            {
                Source = e.ApplicationMessage.Topic,
                RawPayload = e.ApplicationMessage.PayloadSegment.ToArray(),
                Metadata = new Dictionary<string, object>
                {
                    ["mqtt.qos"] = e.ApplicationMessage.QualityOfServiceLevel,
                    ["mqtt.retain"] = e.ApplicationMessage.Retain,
                    ["mqtt.clientId"] = e.ClientId,
                    ["mqtt.contentType"] = e.ApplicationMessage.ContentType ?? string.Empty,
                    ["mqtt.responseTopic"] = e.ApplicationMessage.ResponseTopic ?? string.Empty
                }
            };

            await pipeline.ProcessAsync(context);
        };

        return server;
    }
}
