using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace MessageValidation;

/// <summary>
/// Internal cache of compiled delegates used by the validation and handler-dispatch
/// middlewares to invoke generic <see cref="IMessageValidator{TMessage}"/> and
/// <see cref="IMessageHandler{TMessage}"/> services without per-message reflection.
/// </summary>
internal static class MessageDispatchCache
{
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<MessageValidationResult>>> _validateDelegates = new();
    private static readonly ConcurrentDictionary<Type, Func<object, object, MessageContext, CancellationToken, Task>> _handleDelegates = new();

    public static Func<object, object, CancellationToken, Task<MessageValidationResult>> GetValidateDelegate(Type messageType)
        => _validateDelegates.GetOrAdd(messageType, BuildValidateDelegate);

    public static Func<object, object, MessageContext, CancellationToken, Task> GetHandleDelegate(Type messageType)
        => _handleDelegates.GetOrAdd(messageType, BuildHandleDelegate);

    private static Func<object, object, CancellationToken, Task<MessageValidationResult>> BuildValidateDelegate(Type messageType)
    {
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
