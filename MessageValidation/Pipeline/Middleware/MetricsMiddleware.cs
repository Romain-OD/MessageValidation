using System.Diagnostics;

namespace MessageValidation;

/// <summary>
/// Outermost middleware that records the <c>Processed</c> counter and the total
/// pipeline <c>Duration</c> histogram for every message, regardless of outcome.
/// </summary>
public sealed class MetricsMiddleware(MessageValidationMetrics metrics) : IMessageMiddleware
{
    public async Task InvokeAsync(MessageContext context, MessageDelegate next, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        metrics.RecordProcessed(context.Source);
        try
        {
            await next(context, ct).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            metrics.RecordDuration(context.Source, sw.Elapsed.TotalMilliseconds);
        }
    }
}
