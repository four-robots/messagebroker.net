namespace MessageBroker.Core.Validation;

/// <summary>
/// Represents the result of validating a configuration.
/// Contains any validation errors or warnings that were found.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation passed with no errors.
    /// </summary>
    public bool IsValid => !Errors.Any(e => e.Severity == ValidationSeverity.Error);

    /// <summary>
    /// Gets the list of validation errors and warnings.
    /// </summary>
    public List<ValidationError> Errors { get; } = new();

    /// <summary>
    /// Adds a validation error to the result.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="message">The error message.</param>
    /// <param name="severity">The severity of the validation issue. Defaults to Error.</param>
    public void AddError(string propertyName, string message, ValidationSeverity severity = ValidationSeverity.Error)
    {
        Errors.Add(new ValidationError
        {
            PropertyName = propertyName,
            ErrorMessage = message,
            Severity = severity
        });
    }

    /// <summary>
    /// Gets all errors with Error severity.
    /// </summary>
    /// <returns>A list of critical errors.</returns>
    public IReadOnlyList<ValidationError> GetErrors()
    {
        return Errors.Where(e => e.Severity == ValidationSeverity.Error).ToList();
    }

    /// <summary>
    /// Gets all errors with Warning severity.
    /// </summary>
    /// <returns>A list of warnings.</returns>
    public IReadOnlyList<ValidationError> GetWarnings()
    {
        return Errors.Where(e => e.Severity == ValidationSeverity.Warning).ToList();
    }

    /// <summary>
    /// Gets a formatted summary of all validation errors and warnings.
    /// </summary>
    /// <returns>A human-readable string describing all validation issues.</returns>
    public string GetSummary()
    {
        if (IsValid && Errors.Count == 0)
            return "Validation passed with no issues.";

        var summary = new System.Text.StringBuilder();

        var errors = GetErrors();
        if (errors.Count > 0)
        {
            summary.AppendLine($"Errors ({errors.Count}):");
            foreach (var error in errors)
            {
                summary.AppendLine($"  - {error.PropertyName}: {error.ErrorMessage}");
            }
        }

        var warnings = GetWarnings();
        if (warnings.Count > 0)
        {
            if (errors.Count > 0)
                summary.AppendLine();

            summary.AppendLine($"Warnings ({warnings.Count}):");
            foreach (var warning in warnings)
            {
                summary.AppendLine($"  - {warning.PropertyName}: {warning.ErrorMessage}");
            }
        }

        return summary.ToString().TrimEnd();
    }

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    /// <returns>A ValidationResult indicating success.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult();
    }
}
