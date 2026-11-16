using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.Examples;

/// <summary>
/// Records what the pipeline did with each message so every example doubles as a smoke test:
/// <see cref="WaitAsync"/> returns exit code 0 only when the expected messages were handled and rejected.
/// </summary>
public sealed class Demo(int expectedValid, int expectedInvalid)
{
    private int _valid;
    private int _invalid;
    private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Valid(MessageContext context, object message)
    {
        Console.WriteLine($"✔ [{context.Source}] {message}");
        Interlocked.Increment(ref _valid);
        Check();
    }

    public void Invalid(MessageContext context, MessageValidationResult result)
    {
        Console.WriteLine($"✘ [{context.Source}] {string.Join("; ", result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))}");
        Interlocked.Increment(ref _invalid);
        Check();
    }

    /// <summary>Waits for the expected outcomes and returns the process exit code.</summary>
    public async Task<int> WaitAsync(TimeSpan? timeout = null)
    {
        try
        {
            await _done.Task.WaitAsync(timeout ?? TimeSpan.FromMinutes(2));
        }
        catch (TimeoutException)
        {
        }

        // >= rather than ==: a broker may replay messages left over from a previous run.
        var passed = _valid >= expectedValid && _invalid >= expectedInvalid;
        Console.WriteLine($"{(passed ? "PASS" : "FAIL")}: {_valid}/{expectedValid} handled, {_invalid}/{expectedInvalid} rejected");
        return passed ? 0 : 1;
    }

    private void Check()
    {
        if (Volatile.Read(ref _valid) >= expectedValid && Volatile.Read(ref _invalid) >= expectedInvalid)
            _done.TrySetResult();
    }

    /// <summary>Reads connection settings injected by compose.yaml, falling back to a local broker.</summary>
    public static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

    public static byte[] Json<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value);

    public static MessageContext Context<T>(string source, T message) => new() { Source = source, RawPayload = Json(message) };

    /// <summary>Retries until the broker container accepts connections (containers start in parallel).</summary>
    public static async Task<T> RetryAsync<T>(Func<Task<T>> action)
    {
        var deadline = DateTime.UtcNow.AddMinutes(3);
        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (DateTime.UtcNow < deadline)
            {
                Console.WriteLine($"… waiting for broker ({ex.GetType().Name}: {ex.Message.Split('\n')[0]})");
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }

    public static Task RetryAsync(Func<Task> action) => RetryAsync(async () => { await action(); return true; });
}

/// <summary>Handles every valid message: the open-generic registration closes over any mapped type.</summary>
public sealed class DemoHandler<TMessage>(Demo demo) : IMessageHandler<TMessage> where TMessage : class
{
    public Task HandleAsync(TMessage message, MessageContext context, CancellationToken ct = default)
    {
        demo.Valid(context, message);
        return Task.CompletedTask;
    }
}

/// <summary>Receives rejected messages when <see cref="FailureBehavior.Custom"/> is configured.</summary>
public sealed class DemoFailureHandler(Demo demo) : IValidationFailureHandler
{
    public Task HandleAsync(MessageValidationResult result, MessageContext context, CancellationToken ct = default)
    {
        demo.Invalid(context, result);
        return Task.CompletedTask;
    }
}

public sealed class JsonMessageDeserializer : IMessageDeserializer
{
    public object Deserialize(byte[] payload, Type targetType) =>
        JsonSerializer.Deserialize(payload, targetType)
        ?? throw new InvalidOperationException($"Failed to deserialize to {targetType.Name}");
}

public static class DemoServiceCollectionExtensions
{
    /// <summary>Registers the JSON deserializer plus the handlers that report into <paramref name="demo"/>.</summary>
    public static IServiceCollection AddDemo(this IServiceCollection services, Demo demo)
    {
        services.AddSingleton(demo);
        services.AddMessageDeserializer<JsonMessageDeserializer>();
        services.AddValidationFailureHandler<DemoFailureHandler>();
        services.AddScoped(typeof(IMessageHandler<>), typeof(DemoHandler<>));
        return services;
    }
}
