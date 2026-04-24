namespace MessageValidation;

/// <summary>
/// Resolves the <see cref="IMessageValidator{TMessage}"/> for the deserialized message and
/// runs it. Populates <see cref="MessageContext.ValidationResult"/> and records the
/// <c>Failed</c> metric when validation fails. Always calls <c>next</c>; branching based on
/// the outcome is the responsibility of <see cref="FailureHandlingMiddleware"/>.
/// </summary>
public sealed class ValidationMiddleware(MessageValidationMetrics metrics) : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct)
    {
        if (context.MessageType is null || context.Message is null || context.Services is null)
        {
            await next(context, ct).ConfigureAwait(false);
            return;
        }

        var validatorType = typeof(IMessageValidator<>).MakeGenericType(context.MessageType);
        if (context.Services.GetService(validatorType) is { } validatorObj)
        {
            var validate = MessageDispatchCache.GetValidateDelegate(context.MessageType);
            var result = await validate(validatorObj, context.Message, ct).ConfigureAwait(false);
            context.ValidationResult = result;

            if (!result.IsValid)
                metrics.RecordFailed(context.Source);
        }

        await next(context, ct).ConfigureAwait(false);
    }
}
