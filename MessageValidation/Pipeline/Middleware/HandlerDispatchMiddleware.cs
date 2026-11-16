namespace MessageValidation;

/// <summary>
/// Terminal middleware that resolves and invokes <see cref="IMessageHandler{TMessage}"/>
/// for the deserialized, validated message, and records the <c>Succeeded</c> metric.
/// </summary>
public sealed class HandlerDispatchMiddleware(MessageValidationMetrics metrics) : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct)
    {
        if (context.MessageType is not null && context.Message is not null && context.Services is not null)
        {
            var dispatch = MessageDispatchCache.Get(context.MessageType);
            if (context.Services.GetService(dispatch.HandlerServiceType) is { } handler)
            {
                await dispatch.Handle(handler, context.Message, context, ct).ConfigureAwait(false);
            }
        }

        metrics.RecordSucceeded(context.Source);
        await next(context, ct).ConfigureAwait(false);
    }
}
