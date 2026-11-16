using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.OnnxRuntime;

namespace MessageValidation.OnnxRuntime;

/// <summary>
/// Extension methods for registering an ONNX model as the <see cref="IMessageValidator{TMessage}"/>
/// for a message type.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OnnxMessageValidator{TMessage}"/> as the singleton validator for
    /// <typeparamref name="TMessage"/>. The model is loaded once, on first use.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="modelPath">Path to the <c>.onnx</c> model file.</param>
    /// <param name="features">Maps a message to the model's input feature vector.</param>
    /// <param name="threshold">Scores strictly above this value fail validation.</param>
    /// <param name="configureSession">
    /// Optional hook to tune <see cref="SessionOptions"/> — thread count, graph optimization,
    /// or hardware execution providers (CUDA, DirectML, OpenVINO, QNN…) available on the device.
    /// </param>
    /// <remarks>
    /// This closed registration takes precedence over open-generic adapters
    /// (e.g. <c>AddMessageFluentValidation</c>) for <typeparamref name="TMessage"/>.
    /// </remarks>
    public static IServiceCollection AddMessageOnnxValidation<TMessage>(
        this IServiceCollection services,
        string modelPath,
        Func<TMessage, float[]> features,
        float threshold,
        Action<SessionOptions>? configureSession = null)
        where TMessage : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);
        ArgumentNullException.ThrowIfNull(features);

        services.AddSingleton<IMessageValidator<TMessage>>(_ =>
        {
            using var sessionOptions = new SessionOptions();
            configureSession?.Invoke(sessionOptions);
            return new OnnxMessageValidator<TMessage>(new InferenceSession(modelPath, sessionOptions), features, threshold);
        });

        return services;
    }
}
