using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MessageValidation;

/// <summary>
/// Internal cache of typed invokers used by the validation and handler-dispatch
/// middlewares to call generic <see cref="IMessageValidator{TMessage}"/> and
/// <see cref="IMessageHandler{TMessage}"/> services without per-message reflection.
/// Types mapped via <see cref="MessageValidationOptions.MapSource{TMessage}"/> are
/// registered statically, which keeps the hot path trim- and Native AOT-safe.
/// </summary>
internal static class MessageDispatchCache
{
    internal sealed record Entry(
        Type ValidatorServiceType,
        Type HandlerServiceType,
        Func<object, object, CancellationToken, Task<MessageValidationResult>> Validate,
        Func<object, object, MessageContext, CancellationToken, Task> Handle);

    private static readonly ConcurrentDictionary<Type, Entry> _entries = new();

    private static readonly MethodInfo _createMethod =
        typeof(MessageDispatchCache).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static void Register<TMessage>() where TMessage : class
        => _entries.TryAdd(typeof(TMessage), Create<TMessage>());

    public static Entry Get(Type messageType)
        => _entries.TryGetValue(messageType, out var entry) ? entry : _entries.GetOrAdd(messageType, CreateViaReflection);

    private static Entry Create<TMessage>() where TMessage : class => new(
        typeof(IMessageValidator<TMessage>),
        typeof(IMessageHandler<TMessage>),
        (validator, message, ct) => ((IMessageValidator<TMessage>)validator).ValidateAsync((TMessage)message, ct),
        (handler, message, context, ct) => ((IMessageHandler<TMessage>)handler).HandleAsync((TMessage)message, context, ct));

    // Only reached when custom middleware sets MessageContext.MessageType to a type that was never
    // passed to MapSource<T>. Message types are reference types, so the shared canonical
    // instantiation of Create<T> is always available under Native AOT.
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "TMessage is constrained to reference types; shared generic code exists.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Create<T> has no trimming-sensitive requirements on T.")]
    private static Entry CreateViaReflection(Type messageType)
        => (Entry)_createMethod.MakeGenericMethod(messageType).Invoke(null, null)!;
}
