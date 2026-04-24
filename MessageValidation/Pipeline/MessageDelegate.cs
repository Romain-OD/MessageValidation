namespace MessageValidation;

/// <summary>
/// A function that can process a <see cref="MessageContext"/> as part of a middleware pipeline.
/// </summary>
/// <param name="context">The message context flowing through the pipeline.</param>
/// <param name="ct">A token to cancel the asynchronous operation.</param>
/// <returns>A <see cref="Task"/> that completes when processing is done.</returns>
public delegate Task MessageDelegate(MessageContext context, CancellationToken ct);
