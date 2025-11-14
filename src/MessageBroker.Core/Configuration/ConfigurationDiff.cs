namespace MessageBroker.Core.Configuration;

/// <summary>
/// Represents the differences between two broker configurations.
/// Used to track what changed when a configuration is updated.
/// </summary>
public class ConfigurationDiff
{
    /// <summary>
    /// Gets or sets the list of property changes between configurations.
    /// </summary>
    public List<PropertyChange> Changes { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether there are any changes.
    /// </summary>
    public bool HasChanges => Changes.Count > 0;

    /// <summary>
    /// Gets a list of property names that were changed.
    /// </summary>
    /// <returns>A list of property names that have different values.</returns>
    public IReadOnlyList<string> GetChangedProperties()
    {
        return Changes.Select(c => c.PropertyName).ToList();
    }

    /// <summary>
    /// Gets a formatted summary of all changes.
    /// </summary>
    /// <returns>A human-readable string describing all changes.</returns>
    public string GetSummary()
    {
        if (!HasChanges)
            return "No changes";

        return string.Join(Environment.NewLine,
            Changes.Select(c => $"{c.PropertyName}: {c.OldValue ?? "null"} → {c.NewValue ?? "null"}"));
    }
}

/// <summary>
/// Represents a change to a single property in a configuration.
/// </summary>
public class PropertyChange
{
    /// <summary>
    /// Gets or sets the name of the property that changed.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous value of the property.
    /// Null if the property had no value or was not set.
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value of the property.
    /// Null if the property is being cleared or removed.
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// Returns a string representation of this property change.
    /// </summary>
    public override string ToString()
    {
        return $"{PropertyName}: {OldValue ?? "null"} → {NewValue ?? "null"}";
    }
}
