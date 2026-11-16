# MessageValidation.OnnxRuntime

Edge-AI validation for the [MessageValidation](https://github.com/Romain-OD/MessageValidation) pipeline:
score every message with a **local ONNX model** (anomaly detector, outlier model…) and reject it when
the score exceeds a threshold — on the device, with no cloud round-trip.

```bash
dotnet add package MessageValidation.OnnxRuntime
```

## Usage

```csharp
services.AddMessageValidation(o => o.MapSource<TemperatureReading>("sensors/+/temperature"));
services.AddMessageDeserializer<JsonMessageDeserializer>();

services.AddMessageOnnxValidation<TemperatureReading>(
    modelPath: "models/temperature-anomaly.onnx",
    features: r => [(float)r.Value, r.Humidity],
    threshold: 0.8f);

services.AddMessageHandler<TemperatureReading, TemperatureHandler>();
```

Readings scoring above `0.8` fail validation with a single error on property `"$anomaly"`
(`OnnxMessageValidator<T>.AnomalyPropertyName`) and are routed through the configured
`FailureBehavior` (log, dead-letter, custom…), just like a rule-based failure.

## Model contract

| | |
|---|---|
| Input | First model input, `float` tensor of shape `[1, n]` built from `features(message)` |
| Output | First model output; its first element is read as the `float` score |
| Decision | `score <= threshold` → valid; otherwise (including `NaN`) → invalid |

Any model exported to ONNX with that shape works: scikit-learn (`skl2onnx`) IsolationForest /
OneClassSVM wrapped to emit a score, PyTorch/TensorFlow autoencoders emitting reconstruction error, etc.

## Hardware acceleration

The model loads once (singleton `InferenceSession`, thread-safe). Tune it for the device:

```csharp
services.AddMessageOnnxValidation<TemperatureReading>(path, features, 0.8f,
    configureSession: o =>
    {
        o.IntraOpNumThreads = 1;          // small edge boards
        // o.AppendExecutionProvider_CUDA();  // Jetson, with the GPU runtime package
    });
```

The package depends on the CPU `Microsoft.ML.OnnxRuntime` runtime (win/linux/macOS, x64 and arm64,
incl. Raspberry Pi 64-bit). Swap in a GPU/NPU runtime package in your app if needed.

## Notes

- This is a closed `IMessageValidator<T>` registration, so for `T` it takes precedence over
  open-generic adapters such as `AddMessageFluentValidation`.
- Trim / Native AOT compatible.
