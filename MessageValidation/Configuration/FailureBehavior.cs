namespace MessageValidation;

/// <summary>
/// Determines how validation failures are handled.
/// </summary>
public enum FailureBehavior
{
    Log,
    DeadLetter,
    Skip,
    ThrowException,
    Custom
}
