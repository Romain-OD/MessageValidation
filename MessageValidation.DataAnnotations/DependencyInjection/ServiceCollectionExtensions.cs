using Microsoft.Extensions.DependencyInjection;

namespace MessageValidation.DataAnnotations;

/// <summary>
/// Extension methods for registering DataAnnotations as the validation adapter
/// for the MessageValidation pipeline.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DataAnnotations adapter as the open-generic
    /// <see cref="IMessageValidator{TMessage}"/> implementation so that any message type
    /// decorated with <see cref="System.ComponentModel.DataAnnotations"/> attributes
    /// is automatically validated by the pipeline.
    /// </summary>
    public static IServiceCollection AddMessageDataAnnotationsValidation(
        this IServiceCollection services)
    {
        services.AddScoped(typeof(IMessageValidator<>), typeof(DataAnnotationsMessageValidator<>));
        return services;
    }
}
