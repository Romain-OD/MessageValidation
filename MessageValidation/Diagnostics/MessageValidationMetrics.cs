using System.Diagnostics.Metrics;

namespace MessageValidation;

/// <summary>
/// Exposes pipeline observability metrics via <see cref="System.Diagnostics.Metrics"/>.
/// </summary>
public sealed class MessageValidationMetrics
{
    /// <summary>
    /// The meter name used for all pipeline metrics.
    /// </summary>
    public const string MeterName = "MessageValidation";

    private readonly Counter<long> _messagesProcessed;
    private readonly Counter<long> _validationSucceeded;
    private readonly Counter<long> _validationFailed;
    private readonly Counter<long> _unmappedSources;
    private readonly Counter<long> _deadLettered;
    private readonly Histogram<double> _processingDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageValidationMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory used to create metrics.</param>
    public MessageValidationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _messagesProcessed = meter.CreateCounter<long>(
            "messaging.validation.messages_processed",
            description: "Total messages entering the pipeline");

        _validationSucceeded = meter.CreateCounter<long>(
            "messaging.validation.succeeded",
            description: "Messages that passed validation");

        _validationFailed = meter.CreateCounter<long>(
            "messaging.validation.failed",
            description: "Messages that failed validation");

        _unmappedSources = meter.CreateCounter<long>(
            "messaging.validation.unmapped",
            description: "Messages with no source mapping");

        _deadLettered = meter.CreateCounter<long>(
            "messaging.validation.dead_lettered",
            description: "Messages routed to a dead-letter destination");

        _processingDuration = meter.CreateHistogram<double>(
            "messaging.validation.duration",
            unit: "ms",
            description: "Pipeline processing duration");
    }

    internal void RecordProcessed(string source) =>
        _messagesProcessed.Add(1, new KeyValuePair<string, object?>("source", source));

    internal void RecordSucceeded(string source) =>
        _validationSucceeded.Add(1, new KeyValuePair<string, object?>("source", source));

    internal void RecordFailed(string source) =>
        _validationFailed.Add(1, new KeyValuePair<string, object?>("source", source));

    internal void RecordUnmapped(string source) =>
        _unmappedSources.Add(1, new KeyValuePair<string, object?>("source", source));

    internal void RecordDeadLettered(string source) =>
        _deadLettered.Add(1, new KeyValuePair<string, object?>("source", source));

    internal void RecordDuration(string source, double ms) =>
        _processingDuration.Record(ms, new KeyValuePair<string, object?>("source", source));
}
