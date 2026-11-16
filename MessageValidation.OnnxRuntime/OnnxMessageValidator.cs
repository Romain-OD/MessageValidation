using Microsoft.ML.OnnxRuntime;

namespace MessageValidation.OnnxRuntime;

/// <summary>
/// Validates messages by scoring them with a local ONNX model (e.g. an anomaly detector).
/// The message is turned into a <c>float[1, n]</c> feature tensor, fed to the model's first input,
/// and the first element of the model's first output is read as the score.
/// The message fails validation when the score is above the threshold (or is <c>NaN</c>).
/// </summary>
/// <typeparam name="TMessage">The deserialized message type.</typeparam>
public sealed class OnnxMessageValidator<TMessage> : IMessageValidator<TMessage>, IDisposable
    where TMessage : class
{
    /// <summary>Property name used on the <see cref="MessageValidationError"/> returned for anomalous messages.</summary>
    public const string AnomalyPropertyName = "$anomaly";

    private readonly InferenceSession _session;
    private readonly RunOptions _runOptions = new();
    private readonly Func<TMessage, float[]> _features;
    private readonly float _threshold;
    private readonly string[] _inputNames;
    private readonly string[] _outputNames;

    /// <summary>Creates a validator over an already-loaded <see cref="InferenceSession"/>. The validator owns and disposes it.</summary>
    /// <param name="session">The ONNX inference session (thread-safe; shared across messages).</param>
    /// <param name="features">Maps a message to its feature vector.</param>
    /// <param name="threshold">Scores strictly above this value are rejected.</param>
    public OnnxMessageValidator(InferenceSession session, Func<TMessage, float[]> features, float threshold)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _threshold = threshold;
        _inputNames = [session.InputNames[0]];
        _outputNames = [session.OutputNames[0]];
    }

    /// <inheritdoc />
    public Task<MessageValidationResult> ValidateAsync(TMessage message, CancellationToken ct = default)
    {
        var score = Score(message);

        return Task.FromResult(score <= _threshold
            ? MessageValidationResult.Success()
            : MessageValidationResult.Failure(
            [
                new MessageValidationError(AnomalyPropertyName, $"Anomaly score {score:F3} exceeds threshold {_threshold:F3}.")
            ]));
    }

    /// <summary>Runs the model and returns the raw score for <paramref name="message"/>.</summary>
    public float Score(TMessage message)
    {
        var features = _features(message);
        using var input = OrtValue.CreateTensorValueFromMemory(features, [1, features.Length]);
        using var outputs = _session.Run(_runOptions, _inputNames, [input], _outputNames);
        return outputs[0].GetTensorDataAsSpan<float>()[0];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _runOptions.Dispose();
        _session.Dispose();
    }
}
