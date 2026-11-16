using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MessageValidation.SparkplugB;

/// <summary>DI registration for Sparkplug B validation.</summary>
public static class SparkplugBServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SparkplugBValidator"/> as the singleton
    /// <see cref="IMessageValidator{TMessage}"/> for <see cref="SparkplugBMessage"/>.
    /// Also registers a fallback <see cref="IMessageDeserializer"/> when none is present,
    /// so Sparkplug-only apps don't need one. Pair with
    /// <see cref="SparkplugBPipelineBuilderExtensions.UseSparkplugB"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="trackSessionState">Track seq, bdSeq and birth metric types per edge node.</param>
    public static IServiceCollection AddSparkplugBValidation(this IServiceCollection services, bool trackSessionState = true)
    {
        services.AddSingleton<IMessageValidator<SparkplugBMessage>>(new SparkplugBValidator(trackSessionState));
        services.TryAddSingleton<IMessageDeserializer, UnsupportedDeserializer>();
        return services;
    }

    private sealed class UnsupportedDeserializer : IMessageDeserializer
    {
        public object Deserialize(byte[] payload, Type targetType) =>
            throw new NotSupportedException(
                $"No IMessageDeserializer registered for {targetType.Name}. Register one with AddMessageDeserializer<T>().");
    }
}
