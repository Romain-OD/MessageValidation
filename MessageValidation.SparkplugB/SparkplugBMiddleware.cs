namespace MessageValidation.SparkplugB;

/// <summary>
/// Turns <see cref="MessageContext.RawPayload"/> into a <see cref="SparkplugBMessage"/> for sources
/// mapped to <see cref="SparkplugBMessage"/>. Must run before the default
/// <see cref="DeserializationMiddleware"/>, which then skips the already-populated message.
/// Add it with <see cref="SparkplugBPipelineBuilderExtensions.UseSparkplugB"/>.
/// </summary>
public sealed class SparkplugBMiddleware(MessageValidationOptions options) : IMessageMiddleware
{
    public Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct)
    {
        if (context.Message is null
            && options.TryResolveMessageType(context.Source, out var type)
            && type == typeof(SparkplugBMessage))
        {
            context.Message = SparkplugBMessage.Parse(context.Source, context.RawPayload);
        }

        return next(context, ct);
    }
}

/// <summary>Pipeline builder extensions for Sparkplug B.</summary>
public static class SparkplugBPipelineBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="SparkplugBMiddleware"/>. Call before
    /// <see cref="MessageValidationPipeline.ConfigureDefaults"/>.
    /// </summary>
    public static IMessagePipelineBuilder UseSparkplugB(this IMessagePipelineBuilder builder) =>
        builder.UseMiddleware<SparkplugBMiddleware>();
}
