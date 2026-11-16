using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.OnnxRuntime.Tests;

// Models/max_score.onnx returns max(features) — a stand-in anomaly score (see Models/gen_model.py).
public class OnnxMessageValidatorTests
{
    private static readonly string ModelPath = Path.Combine(AppContext.BaseDirectory, "Models", "max_score.onnx");

    public sealed class Reading
    {
        public float A { get; set; }
        public float B { get; set; }
    }

    [Theory]
    [InlineData(0.2f, 0.7f, true)]
    [InlineData(0.8f, 0.8f, true)]   // equal to threshold passes
    [InlineData(0.1f, 0.9f, false)]
    [InlineData(float.NaN, 0f, false)]
    public async Task Validate_ComparesModelScoreToThreshold(float a, float b, bool expectedValid)
    {
        await using var sp = BuildServices(handled: null);
        var validator = sp.GetRequiredService<IMessageValidator<Reading>>();

        var result = await validator.ValidateAsync(new Reading { A = a, B = b });

        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
            Assert.Equal(OnnxMessageValidator<Reading>.AnomalyPropertyName, Assert.Single(result.Errors).PropertyName);
    }

    [Theory]
    [InlineData(0.5f, true)]
    [InlineData(5f, false)]
    public async Task Pipeline_OnlyDispatchesNormalReadings(float value, bool expectedHandled)
    {
        var handled = false;
        await using var sp = BuildServices(() => handled = true);
        var pipeline = sp.GetRequiredService<IMessageValidationPipeline>();

        await pipeline.ProcessAsync(new MessageContext
        {
            Source = "sensors/1/reading",
            RawPayload = JsonSerializer.SerializeToUtf8Bytes(new Reading { A = value, B = 0 })
        });

        Assert.Equal(expectedHandled, handled);
    }

    private static ServiceProvider BuildServices(Action? handled)
    {
        var services = new ServiceCollection();
        services.AddMessageValidation(o => o.MapSource<Reading>("sensors/+/reading"));
        services.AddSingleton<IMessageDeserializer, JsonDeserializer>();
        services.AddMessageOnnxValidation<Reading>(ModelPath, r => [r.A, r.B], threshold: 0.8f);
        if (handled is not null)
            services.AddSingleton<IMessageHandler<Reading>>(new CallbackHandler(handled));
        return services.BuildServiceProvider();
    }

    private sealed class JsonDeserializer : IMessageDeserializer
    {
        public object Deserialize(byte[] payload, Type targetType) => JsonSerializer.Deserialize(payload, targetType)!;
    }

    private sealed class CallbackHandler(Action onHandle) : IMessageHandler<Reading>
    {
        public Task HandleAsync(Reading message, MessageContext context, CancellationToken ct = default)
        {
            onHandle();
            return Task.CompletedTask;
        }
    }
}
