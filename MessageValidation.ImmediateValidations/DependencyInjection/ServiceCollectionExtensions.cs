using Immediate.Validations.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.ImmediateValidations;

/// <summary>
/// Extension methods for registering Immediate.Validations as the validation adapter
/// for the MessageValidation pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Immediate.Validations adapter as the open-generic
    /// <see cref="IMessageValidator{TMessage}"/> implementation. Any message type
    /// annotated with <c>[Validate]</c> and implementing <see cref="IValidationTarget{T}"/>
    /// will then be validated by the pipeline through its source-generated, AOT-friendly,
    /// zero-reflection <c>Validate</c> method.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMessageImmediateValidations(
        this IServiceCollection services)
    {
        services.AddScoped(typeof(IMessageValidator<>), typeof(ImmediateValidationsMessageValidator<>));
        return services;
    }
}
