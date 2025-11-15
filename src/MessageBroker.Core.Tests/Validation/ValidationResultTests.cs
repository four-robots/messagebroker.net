using MessageBroker.Core.Validation;
using Xunit;

namespace MessageBroker.Core.Tests.Validation;

public class ValidationResultTests
{
    [Fact]
    public void Constructor_CreatesEmptyResult()
    {
        // Act
        var result = new ValidationResult();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_ReturnsValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void AddError_WithError_MakesResultInvalid()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("TestProperty", "Test error message");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("TestProperty", result.Errors[0].PropertyName);
        Assert.Equal("Test error message", result.Errors[0].ErrorMessage);
        Assert.Equal(ValidationSeverity.Error, result.Errors[0].Severity);
    }

    [Fact]
    public void AddError_WithWarning_KeepsResultValid()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        result.AddError("TestProperty", "Test warning", ValidationSeverity.Warning);

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(ValidationSeverity.Warning, result.Errors[0].Severity);
    }

    [Fact]
    public void GetErrors_ReturnsOnlyErrors()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Prop1", "Error 1", ValidationSeverity.Error);
        result.AddError("Prop2", "Warning 1", ValidationSeverity.Warning);
        result.AddError("Prop3", "Error 2", ValidationSeverity.Error);

        // Act
        var errors = result.GetErrors();

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.All(errors, e => Assert.Equal(ValidationSeverity.Error, e.Severity));
    }

    [Fact]
    public void GetWarnings_ReturnsOnlyWarnings()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Prop1", "Error 1", ValidationSeverity.Error);
        result.AddError("Prop2", "Warning 1", ValidationSeverity.Warning);
        result.AddError("Prop3", "Warning 2", ValidationSeverity.Warning);

        // Act
        var warnings = result.GetWarnings();

        // Assert
        Assert.Equal(2, warnings.Count);
        Assert.All(warnings, w => Assert.Equal(ValidationSeverity.Warning, w.Severity));
    }

    [Fact]
    public void GetSummary_WithNoErrors_ReturnsSuccessMessage()
    {
        // Arrange
        var result = new ValidationResult();

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("Validation passed", summary);
    }

    [Fact]
    public void GetSummary_WithErrors_IncludesErrorDetails()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Port", "Invalid port number");
        result.AddError("Host", "Invalid host");

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("Errors (2)", summary);
        Assert.Contains("Port: Invalid port number", summary);
        Assert.Contains("Host: Invalid host", summary);
    }

    [Fact]
    public void GetSummary_WithWarnings_IncludesWarningDetails()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Trace", "Trace without debug", ValidationSeverity.Warning);

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("Warnings (1)", summary);
        Assert.Contains("Trace: Trace without debug", summary);
    }

    [Fact]
    public void GetSummary_WithErrorsAndWarnings_IncludesBoth()
    {
        // Arrange
        var result = new ValidationResult();
        result.AddError("Port", "Invalid port", ValidationSeverity.Error);
        result.AddError("Trace", "Trace without debug", ValidationSeverity.Warning);

        // Act
        var summary = result.GetSummary();

        // Assert
        Assert.Contains("Errors (1)", summary);
        Assert.Contains("Warnings (1)", summary);
        Assert.Contains("Port: Invalid port", summary);
        Assert.Contains("Trace: Trace without debug", summary);
    }
}
