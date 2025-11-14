namespace MessageBroker.Core.Validation;

/// <summary>
/// Represents a single validation error or warning for a configuration property.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the name of the property that failed validation.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message describing why validation failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity of this validation issue.
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;

    /// <summary>
    /// Returns a string representation of this validation error.
    /// </summary>
    public override string ToString()
    {
        return $"[{Severity}] {PropertyName}: {ErrorMessage}";
    }
}

/// <summary>
/// Specifies the severity level of a validation issue.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// A critical error that prevents the configuration from being applied.
    /// </summary>
    Error,

    /// <summary>
    /// A warning about a potential issue that does not prevent configuration application.
    /// </summary>
    Warning
}
