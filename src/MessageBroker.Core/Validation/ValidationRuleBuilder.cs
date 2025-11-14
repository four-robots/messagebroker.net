using System.Linq.Expressions;
using MessageBroker.Core.Configuration;

namespace MessageBroker.Core.Validation;

/// <summary>
/// Provides a fluent API for building custom validation rules for broker configurations.
/// Allows creating reusable, composable validation logic.
/// </summary>
public class ValidationRuleBuilder
{
    private readonly List<ValidationRule> _rules = new();

    /// <summary>
    /// Creates a new rule builder for validating a specific property.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property to validate.</typeparam>
    /// <param name="propertyExpression">An expression that identifies the property to validate.</param>
    /// <returns>A property rule builder for configuring validation rules.</returns>
    public PropertyRuleBuilder<TProperty> ForProperty<TProperty>(
        Expression<Func<BrokerConfiguration, TProperty>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        return new PropertyRuleBuilder<TProperty>(this, propertyName, propertyExpression.Compile());
    }

    /// <summary>
    /// Validates a configuration against all configured rules.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>A validation result containing any errors found.</returns>
    public ValidationResult Validate(BrokerConfiguration config)
    {
        var result = new ValidationResult();

        foreach (var rule in _rules)
        {
            rule.Validate(config, result);
        }

        return result;
    }

    internal void AddRule(ValidationRule rule)
    {
        _rules.Add(rule);
    }

    private static string GetPropertyName<TProperty>(Expression<Func<BrokerConfiguration, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("Expression must be a property access", nameof(expression));
    }
}

/// <summary>
/// Provides a fluent API for building validation rules for a specific property.
/// </summary>
/// <typeparam name="TProperty">The type of the property being validated.</typeparam>
public class PropertyRuleBuilder<TProperty>
{
    private readonly ValidationRuleBuilder _parent;
    private readonly string _propertyName;
    private readonly Func<BrokerConfiguration, TProperty> _propertyAccessor;

    internal PropertyRuleBuilder(
        ValidationRuleBuilder parent,
        string propertyName,
        Func<BrokerConfiguration, TProperty> propertyAccessor)
    {
        _parent = parent;
        _propertyName = propertyName;
        _propertyAccessor = propertyAccessor;
    }

    /// <summary>
    /// Adds a custom validation rule with a predicate.
    /// </summary>
    /// <param name="predicate">A function that returns true if the value is valid.</param>
    /// <param name="errorMessage">The error message to use if validation fails.</param>
    /// <param name="severity">The severity of the validation failure.</param>
    /// <returns>This builder for method chaining.</returns>
    public PropertyRuleBuilder<TProperty> Must(
        Func<TProperty, bool> predicate,
        string errorMessage,
        ValidationSeverity severity = ValidationSeverity.Error)
    {
        var rule = new ValidationRule
        {
            PropertyName = _propertyName,
            Validate = (config, result) =>
            {
                var value = _propertyAccessor(config);
                if (!predicate(value))
                {
                    result.AddError(_propertyName, errorMessage, severity);
                }
            }
        };

        _parent.AddRule(rule);
        return this;
    }

    /// <summary>
    /// Validates that a numeric value is within a specified range.
    /// </summary>
    /// <param name="min">The minimum allowed value (inclusive).</param>
    /// <param name="max">The maximum allowed value (inclusive).</param>
    /// <returns>This builder for method chaining.</returns>
    public PropertyRuleBuilder<TProperty> InRange(int min, int max)
    {
        if (!typeof(IComparable<int>).IsAssignableFrom(typeof(TProperty)))
        {
            throw new InvalidOperationException($"InRange can only be used with numeric properties. Property type is {typeof(TProperty).Name}");
        }

        return Must(
            value =>
            {
                if (value is IComparable<int> comparable)
                {
                    return comparable.CompareTo(min) >= 0 && comparable.CompareTo(max) <= 0;
                }
                return false;
            },
            $"{_propertyName} must be between {min} and {max}");
    }

    /// <summary>
    /// Validates that a string value is not null or empty.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public PropertyRuleBuilder<TProperty> NotNullOrEmpty()
    {
        if (typeof(TProperty) != typeof(string))
        {
            throw new InvalidOperationException("NotNullOrEmpty can only be used with string properties");
        }

        return Must(
            value => !string.IsNullOrEmpty(value as string),
            $"{_propertyName} cannot be null or empty");
    }

    /// <summary>
    /// Validates that a string value is not null, empty, or whitespace.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public PropertyRuleBuilder<TProperty> NotNullOrWhiteSpace()
    {
        if (typeof(TProperty) != typeof(string))
        {
            throw new InvalidOperationException("NotNullOrWhiteSpace can only be used with string properties");
        }

        return Must(
            value => !string.IsNullOrWhiteSpace(value as string),
            $"{_propertyName} cannot be null, empty, or whitespace");
    }

    /// <summary>
    /// Validates that a numeric value is greater than zero.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public PropertyRuleBuilder<TProperty> GreaterThanZero()
    {
        if (!typeof(IComparable<int>).IsAssignableFrom(typeof(TProperty)))
        {
            throw new InvalidOperationException($"GreaterThanZero can only be used with numeric properties. Property type is {typeof(TProperty).Name}");
        }

        return Must(
            value =>
            {
                if (value is IComparable<int> comparable)
                {
                    return comparable.CompareTo(0) > 0;
                }
                return false;
            },
            $"{_propertyName} must be greater than zero");
    }

    /// <summary>
    /// Validates that a value is not equal to a specified value.
    /// </summary>
    /// <param name="comparisonValue">The value to compare against.</param>
    /// <returns>This builder for method chaining.</returns>
    public PropertyRuleBuilder<TProperty> NotEqual(TProperty comparisonValue)
    {
        return Must(
            value => !EqualityComparer<TProperty>.Default.Equals(value, comparisonValue),
            $"{_propertyName} cannot be equal to {comparisonValue}");
    }

    /// <summary>
    /// Returns to the parent builder to configure additional properties.
    /// </summary>
    /// <returns>The parent ValidationRuleBuilder.</returns>
    public ValidationRuleBuilder And()
    {
        return _parent;
    }
}

/// <summary>
/// Represents a validation rule that can be applied to a configuration.
/// </summary>
internal class ValidationRule
{
    public string PropertyName { get; set; } = string.Empty;
    public Action<BrokerConfiguration, ValidationResult> Validate { get; set; } = null!;
}
