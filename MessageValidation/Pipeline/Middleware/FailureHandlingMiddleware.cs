using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageValidation;

/// <summary>
/// Inspects <see cref="MessageContext.ValidationResult"/>; when validation failed, applies
/// the configured <see cref="FailureBehavior"/> and short-circuits the pipeline so the
/// handler is not invoked. When validation succeeded (or was skipped), delegates to
/// <c>next</c>.
/// </summary>
public sealed class FailureHandlingMiddleware(
    MessageValidationOptions options,
    MessageValidationMetrics metrics,
    ILogger<FailureHandlingMiddleware> logger) : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct)
    {
        var result = context.ValidationResult;
        if (result is null || result.IsValid)
        {
            await next(context, ct).ConfigureAwait(false);
            return;
        }

        var sp = context.Services;

        switch (options.DefaultFailureBehavior)
        {
            case FailureBehavior.Skip:
                return;

            case FailureBehavior.Log:
                logger.LogWarning("Validation failed for {Source}: {Errors}",
                    context.Source,
                    string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
                return;

            case FailureBehavior.DeadLetter:
                var destination = $"{options.DeadLetterPrefix}{context.Source}";
                logger.LogWarning("Dead-lettering message from {Source} to {Destination}: {Errors}",
                    context.Source,
                    destination,
                    string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

                metrics.RecordDeadLettered(context.Source);

                var dlContext = new DeadLetterContext
                {
                    Destination = destination,
                    OriginalContext = context,
                    ValidationResult = result
                };

                var dlHandler = sp?.GetService<IDeadLetterHandler>();
                if (dlHandler is not null)
                {
                    await dlHandler.HandleAsync(dlContext, ct).ConfigureAwait(false);
                }
                else
                {
                    var fallbackHandler = sp?.GetService<IValidationFailureHandler>();
                    if (fallbackHandler is not null)
                        await fallbackHandler.HandleAsync(result, context, ct).ConfigureAwait(false);
                    else
                        logger.LogWarning("No IDeadLetterHandler or IValidationFailureHandler registered. Dead-letter message from {Source} was not forwarded.", context.Source);
                }
                return;

            case FailureBehavior.Custom:
                var failureHandler = sp?.GetService<IValidationFailureHandler>();
                if (failureHandler is not null)
                    await failureHandler.HandleAsync(result, context, ct).ConfigureAwait(false);
                return;

            case FailureBehavior.ThrowException:
                throw new MessageValidationException(result);
        }
    }
}
