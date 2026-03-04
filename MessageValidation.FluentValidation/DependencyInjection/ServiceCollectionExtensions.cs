using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MessageValidation.FluentValidation;

/// <summary>
/// Extension methods for registering FluentValidation as the validation adapter
/// for the MessageValidation pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all FluentValidation validators from the specified assemblies
    /// and wires them into the MessageValidation pipeline via the open-generic
    /// <see cref="FluentValidationMessageValidator{TMessage}"/> adapter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">
    /// Assemblies to scan for <see cref="AbstractValidator{T}"/> implementations.
    /// If none are provided, the calling assembly is scanned.
    /// </param>
    public static IServiceCollection AddMessageFluentValidation(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = [Assembly.GetCallingAssembly()];

        services.AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped);

        // Register the open-generic adapter so that any IMessageValidator<T>
        // is resolved via FluentValidation's IValidator<T>.
        services.AddScoped(typeof(IMessageValidator<>), typeof(FluentValidationMessageValidator<>));

        return services;
    }
}
