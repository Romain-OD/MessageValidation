using Microsoft.Extensions.Logging;

namespace MessageValidation;

/// <summary>
/// Resolves the target CLR message type from <see cref="MessageContext.Source"/> using
/// <see cref="MessageValidationOptions.TryResolveMessageType"/>. Short-circuits the
/// pipeline and records the <c>Unmapped</c> metric when no mapping is found.
/// </summary>
public sealed class TypeResolutionMiddleware(
    MessageValidationOptions options,
    MessageValidationMetrics metrics,
    ILogger<TypeResolutionMiddleware> logger) : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct)
    {
        if (!options.TryResolveMessageType(context.Source, out var messageType) || messageType is null)
        {
            logger.LogWarning("No mapping found for source {Source}", context.Source);
            metrics.RecordUnmapped(context.Source);
            return;
        }

        context.MessageType = messageType;
        await next(context, ct).ConfigureAwait(false);
    }
}
