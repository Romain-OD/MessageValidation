namespace MessageValidation;

/// <summary>
/// Represents a middleware component in the <see cref="IMessageValidationPipeline"/>.
/// Implementations inspect or mutate the <see cref="MessageContext"/> and either invoke
/// <paramref name="next"/> to continue the pipeline or short-circuit by returning without
/// calling it.
/// </summary>
public interface IMessageMiddleware
{
    /// <summary>
    /// Executes the middleware logic.
    /// </summary>
    /// <param name="context">The message context.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="ct">A token to cancel the asynchronous operation.</param>
    Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct);
}
