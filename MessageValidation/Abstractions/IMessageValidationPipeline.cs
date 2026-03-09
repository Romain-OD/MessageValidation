namespace MessageValidation;

/// <summary>
/// Defines the core message validation pipeline contract.
/// </summary>
public interface IMessageValidationPipeline
{
    Task ProcessAsync(MessageContext context, CancellationToken ct = default);
}
