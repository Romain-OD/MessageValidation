using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageValidation;

/// <summary>
/// Core pipeline that deserializes, validates, and dispatches messages.
/// </summary>
public sealed class MessageValidationPipeline(
    IServiceScopeFactory scopeFactory,
    MessageValidationOptions options,
    IMessageDeserializer deserializer,
    ILogger<MessageValidationPipeline> logger)
{
    public async Task ProcessAsync(MessageContext context, CancellationToken ct = default)
    {
        if (!options.TryResolveMessageType(context.Source, out var messageType) || messageType is null)
        {
            logger.LogWarning("No mapping found for source {Source}", context.Source);
            return;
        }

        var message = deserializer.Deserialize(context.RawPayload, messageType);

        await using var scope = scopeFactory.CreateAsyncScope();

        // Resolve IMessageValidator<TMessage>
        var validatorType = typeof(IMessageValidator<>).MakeGenericType(messageType);
        if (scope.ServiceProvider.GetService(validatorType) is { } validatorObj)
        {
            var method = validatorType.GetMethod(nameof(IMessageValidator<object>.ValidateAsync))!;
            var result = await (Task<MessageValidationResult>)method.Invoke(validatorObj, [message, ct])!;

            if (!result.IsValid)
            {
                await HandleFailureAsync(scope.ServiceProvider, result, context, ct);
                return;
            }
        }

        // Dispatch to handler
        var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
        if (scope.ServiceProvider.GetService(handlerType) is { } handler)
        {
            var method = handlerType.GetMethod(nameof(IMessageHandler<object>.HandleAsync))!;
            await (Task)method.Invoke(handler, [message, context, ct])!;
        }
    }

    private async Task HandleFailureAsync(
        IServiceProvider sp,
        MessageValidationResult result,
        MessageContext context,
        CancellationToken ct)
    {
        logger.LogWarning("Validation failed for {Source}: {Errors}",
            context.Source,
            string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

        if (options.DefaultFailureBehavior == FailureBehavior.Custom)
        {
            var failureHandler = sp.GetService<IValidationFailureHandler>();
            if (failureHandler is not null)
                await failureHandler.HandleAsync(result, context, ct);
        }

        if (options.DefaultFailureBehavior == FailureBehavior.ThrowException)
        {
            throw new MessageValidationException(result);
        }
    }
}
