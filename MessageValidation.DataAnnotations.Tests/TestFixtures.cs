using System.ComponentModel.DataAnnotations;

namespace MessageValidation.DataAnnotations.Tests;

public class TestMessage
{
    [Required(ErrorMessage = "Name is required.")]
    public string Name { get; set; } = "";

    [Range(1, 100, ErrorMessage = "Value must be between 1 and 100.")]
    public int Value { get; set; }
}

public class ValidatableMessage : IValidatableObject
{
    [Required(ErrorMessage = "Start is required.")]
    public DateTime? Start { get; set; }

    [Required(ErrorMessage = "End is required.")]
    public DateTime? End { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Start.HasValue && End.HasValue && End < Start)
        {
            yield return new ValidationResult(
                "End must be after Start.",
                [nameof(End)]);
        }
    }
}

public class NoAttributesMessage
{
    public string Data { get; set; } = "";
}
