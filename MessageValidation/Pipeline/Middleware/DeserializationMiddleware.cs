namespace MessageValidation;

/// <summary>
/// Deserializes <see cref="MessageContext.RawPayload"/> into the type previously resolved
/// by <see cref="TypeResolutionMiddleware"/> and stores the result in
/// <see cref="MessageContext.Message"/>.
/// </summary>
public sealed class DeserializationMiddleware(IMessageDeserializer deserializer) : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct)
    {
        if (context.MessageType is null)
            return;

        context.Message = deserializer.Deserialize(context.RawPayload, context.MessageType);
        await next(context, ct).ConfigureAwait(false);
    }
}
