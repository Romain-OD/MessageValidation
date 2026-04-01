using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MessageValidation;

/// <summary>
/// Default implementation of <see cref="IMessageValidationPipeline"/>.
/// Orchestrates deserialization via <see cref="IMessageDeserializer"/>,
/// validation via <see cref="IMessageValidator{TMessage}"/>,
/// and dispatch via <see cref="IMessageHandler{TMessage}"/>.
/// Records pipeline metrics via <see cref="MessageValidationMetrics"/>.
/// </summary>
/// <remarks>
/// Registered automatically by <see cref="ServiceCollectionExtensions.AddMessageValidation"/>.
/// Do not instantiate directly; resolve <see cref="IMessageValidationPipeline"/> from the DI container.
/// </remarks>
public sealed class MessageValidationPipeline(
    IServiceScopeFactory scopeFactory,
    MessageValidationOptions options,
    IMessageDeserializer deserializer,
    ILogger<MessageValidationPipeline> logger,
    MessageValidationMetrics metrics) : IMessageValidationPipeline
{
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<MessageValidationResult>>> _validateDelegates = new();
    private static readonly ConcurrentDictionary<Type, Func<object, object, MessageContext, CancellationToken, Task>> _handleDelegates = new();

    public async Task ProcessAsync(MessageContext context, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        metrics.RecordProcessed(context.Source);

        if (!options.TryResolveMessageType(context.Source, out var messageType) || messageType is null)
        {
            logger.LogWarning("No mapping found for source {Source}", context.Source);
            metrics.RecordUnmapped(context.Source);
            sw.Stop();
            metrics.RecordDuration(context.Source, sw.Elapsed.TotalMilliseconds);
            return;
        }

        var message = deserializer.Deserialize(context.RawPayload, messageType);

        await using var scope = scopeFactory.CreateAsyncScope();

        // Resolve IMessageValidator<TMessage>
        var validatorType = typeof(IMessageValidator<>).MakeGenericType(messageType);
        if (scope.ServiceProvider.GetService(validatorType) is { } validatorObj)
        {
            var validateDelegate = _validateDelegates.GetOrAdd(messageType, BuildValidateDelegate);
            var result = await validateDelegate(validatorObj, message, ct);

            if (!result.IsValid)
            {
                metrics.RecordFailed(context.Source);
                await HandleFailureAsync(scope.ServiceProvider, result, context, ct);
                sw.Stop();
                metrics.RecordDuration(context.Source, sw.Elapsed.TotalMilliseconds);
                return;
            }
        }

        // Dispatch to handler
        var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
        if (scope.ServiceProvider.GetService(handlerType) is { } handler)
        {
            var handleDelegate = _handleDelegates.GetOrAdd(messageType, BuildHandleDelegate);
            await handleDelegate(handler, message, context, ct);
        }

        metrics.RecordSucceeded(context.Source);
        sw.Stop();
        metrics.RecordDuration(context.Source, sw.Elapsed.TotalMilliseconds);
    }

    private async Task HandleFailureAsync(
        IServiceProvider sp,
        MessageValidationResult result,
        MessageContext context,
        CancellationToken ct)
    {
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

                var dlHandler = sp.GetService<IDeadLetterHandler>();
                if (dlHandler is not null)
                {
                    await dlHandler.HandleAsync(dlContext, ct);
                }
                else
                {
                    // Backward-compatible fallback to IValidationFailureHandler
                    var fallbackHandler = sp.GetService<IValidationFailureHandler>();
                    if (fallbackHandler is not null)
                        await fallbackHandler.HandleAsync(result, context, ct);
                    else
                        logger.LogWarning("No IDeadLetterHandler or IValidationFailureHandler registered. Dead-letter message from {Source} was not forwarded.", context.Source);
                }
                return;

            case FailureBehavior.Custom:
                var failureHandler = sp.GetService<IValidationFailureHandler>();
                if (failureHandler is not null)
                    await failureHandler.HandleAsync(result, context, ct);
                return;

            case FailureBehavior.ThrowException:
                throw new MessageValidationException(result);
        }
    }

    private static Func<object, object, CancellationToken, Task<MessageValidationResult>> BuildValidateDelegate(Type messageType)
    {
        // Build: (object validator, object message, CancellationToken ct) =>
        //            ((IMessageValidator<T>)validator).ValidateAsync((T)message, ct)
        var validatorParam = Expression.Parameter(typeof(object), "validator");
        var messageParam = Expression.Parameter(typeof(object), "message");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var validatorType = typeof(IMessageValidator<>).MakeGenericType(messageType);
        var method = validatorType.GetMethod(nameof(IMessageValidator<object>.ValidateAsync))!;

        var call = Expression.Call(
            Expression.Convert(validatorParam, validatorType),
            method,
            Expression.Convert(messageParam, messageType),
            ctParam);

        return Expression.Lambda<Func<object, object, CancellationToken, Task<MessageValidationResult>>>(
            call, validatorParam, messageParam, ctParam).Compile();
    }

    private static Func<object, object, MessageContext, CancellationToken, Task> BuildHandleDelegate(Type messageType)
    {
        // Build: (object handler, object message, MessageContext context, CancellationToken ct) =>
        //            ((IMessageHandler<T>)handler).HandleAsync((T)message, context, ct)
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var messageParam = Expression.Parameter(typeof(object), "message");
        var contextParam = Expression.Parameter(typeof(MessageContext), "context");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
        var method = handlerType.GetMethod(nameof(IMessageHandler<object>.HandleAsync))!;

        var call = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            method,
            Expression.Convert(messageParam, messageType),
            contextParam,
            ctParam);

        return Expression.Lambda<Func<object, object, MessageContext, CancellationToken, Task>>(
            call, handlerParam, messageParam, contextParam, ctParam).Compile();
    }
}
