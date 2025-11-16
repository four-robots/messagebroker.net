using System.Reflection;

namespace MessageBroker.Core.Configuration;

/// <summary>
/// Provides methods for calculating differences between broker configurations.
/// Uses reflection to compare property values and identify changes.
/// </summary>
public static class ConfigurationDiffEngine
{
    /// <summary>
    /// Calculates the differences between two broker configurations.
    /// </summary>
    /// <param name="oldConfig">The original configuration.</param>
    /// <param name="newConfig">The new configuration to compare against.</param>
    /// <returns>A ConfigurationDiff containing all detected changes.</returns>
    public static ConfigurationDiff CalculateDiff(BrokerConfiguration? oldConfig, BrokerConfiguration? newConfig)
    {
        var diff = new ConfigurationDiff();

        // Handle null cases
        if (oldConfig == null && newConfig == null)
        {
            return diff;
        }

        if (oldConfig == null || newConfig == null)
        {
            diff.Changes.Add(new PropertyChange
            {
                PropertyName = "Configuration",
                OldValue = oldConfig,
                NewValue = newConfig
            });
            return diff;
        }

        // Compare all properties using reflection
        var properties = typeof(BrokerConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Skip metadata properties that always change
            if (property.Name == nameof(BrokerConfiguration.ConfigurationId) ||
                property.Name == nameof(BrokerConfiguration.CreatedAt))
            {
                continue;
            }

            var oldValue = property.GetValue(oldConfig);
            var newValue = property.GetValue(newConfig);

            // Handle complex types separately
            if (property.PropertyType == typeof(AuthConfiguration))
            {
                CompareAuthConfiguration(
                    (AuthConfiguration?)oldValue,
                    (AuthConfiguration?)newValue,
                    diff);
            }
            else if (property.PropertyType == typeof(LeafNodeConfiguration))
            {
                CompareLeafNodeConfiguration(
                    (LeafNodeConfiguration?)oldValue,
                    (LeafNodeConfiguration?)newValue,
                    diff);
            }
            else if (property.PropertyType == typeof(ClusterConfiguration))
            {
                CompareClusterConfiguration(
                    (ClusterConfiguration?)oldValue,
                    (ClusterConfiguration?)newValue,
                    diff);
            }
            else if (!AreEqual(oldValue, newValue))
            {
                diff.Changes.Add(new PropertyChange
                {
                    PropertyName = property.Name,
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }

        return diff;
    }

    private static void CompareAuthConfiguration(
        AuthConfiguration? oldAuth,
        AuthConfiguration? newAuth,
        ConfigurationDiff diff)
    {
        if (oldAuth == null && newAuth == null)
            return;

        if (oldAuth == null || newAuth == null)
        {
            diff.Changes.Add(new PropertyChange
            {
                PropertyName = "Auth",
                OldValue = oldAuth,
                NewValue = newAuth
            });
            return;
        }

        var properties = typeof(AuthConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var oldValue = property.GetValue(oldAuth);
            var newValue = property.GetValue(newAuth);

            if (!AreEqual(oldValue, newValue))
            {
                diff.Changes.Add(new PropertyChange
                {
                    PropertyName = $"Auth.{property.Name}",
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }
    }

    private static void CompareLeafNodeConfiguration(
        LeafNodeConfiguration? oldLeafNode,
        LeafNodeConfiguration? newLeafNode,
        ConfigurationDiff diff)
    {
        if (oldLeafNode == null && newLeafNode == null)
            return;

        if (oldLeafNode == null || newLeafNode == null)
        {
            diff.Changes.Add(new PropertyChange
            {
                PropertyName = "LeafNode",
                OldValue = oldLeafNode,
                NewValue = newLeafNode
            });
            return;
        }

        var properties = typeof(LeafNodeConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var oldValue = property.GetValue(oldLeafNode);
            var newValue = property.GetValue(newLeafNode);

            if (!AreEqual(oldValue, newValue))
            {
                diff.Changes.Add(new PropertyChange
                {
                    PropertyName = $"LeafNode.{property.Name}",
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }
    }

    private static void CompareClusterConfiguration(
        ClusterConfiguration? oldCluster,
        ClusterConfiguration? newCluster,
        ConfigurationDiff diff)
    {
        if (oldCluster == null && newCluster == null)
            return;

        if (oldCluster == null || newCluster == null)
        {
            diff.Changes.Add(new PropertyChange
            {
                PropertyName = "Cluster",
                OldValue = oldCluster,
                NewValue = newCluster
            });
            return;
        }

        var properties = typeof(ClusterConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var oldValue = property.GetValue(oldCluster);
            var newValue = property.GetValue(newCluster);

            if (!AreEqual(oldValue, newValue))
            {
                diff.Changes.Add(new PropertyChange
                {
                    PropertyName = $"Cluster.{property.Name}",
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }
    }

    private static bool AreEqual(object? oldValue, object? newValue)
    {
        // Handle null cases
        if (oldValue == null && newValue == null)
            return true;

        if (oldValue == null || newValue == null)
            return false;

        // Handle collections
        if (oldValue is System.Collections.IEnumerable oldEnumerable &&
            newValue is System.Collections.IEnumerable newEnumerable &&
            oldValue is not string)
        {
            return AreCollectionsEqual(oldEnumerable, newEnumerable);
        }

        // Use default equality comparison
        return oldValue.Equals(newValue);
    }

    private static bool AreCollectionsEqual(System.Collections.IEnumerable oldCollection, System.Collections.IEnumerable newCollection)
    {
        var oldList = oldCollection.Cast<object>().ToList();
        var newList = newCollection.Cast<object>().ToList();

        if (oldList.Count != newList.Count)
            return false;

        // Order-sensitive comparison
        for (int i = 0; i < oldList.Count; i++)
        {
            if (!Equals(oldList[i], newList[i]))
                return false;
        }

        return true;
    }
}
