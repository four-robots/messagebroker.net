using MessageBroker.Core.Configuration;
using MessageBroker.Core.Validation;

namespace MessageBroker.Core.Interfaces;

/// <summary>
/// Defines the contract for validating broker configurations.
/// Implementations should validate both individual configurations and changes between configurations.
/// </summary>
public interface IConfigurationValidator
{
    /// <summary>
    /// Validates a broker configuration for correctness and consistency.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>A validation result containing any errors or warnings found.</returns>
    ValidationResult Validate(BrokerConfiguration config);

    /// <summary>
    /// Validates that changes from one configuration to another are safe and valid.
    /// This allows for validation of transitions that might be invalid even if both configurations are individually valid.
    /// </summary>
    /// <param name="current">The current configuration.</param>
    /// <param name="proposed">The proposed new configuration.</param>
    /// <returns>A validation result containing any errors or warnings about the proposed changes.</returns>
    ValidationResult ValidateChanges(BrokerConfiguration current, BrokerConfiguration proposed);
}
