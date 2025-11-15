using MessageBroker.Core.Configuration;
using Xunit;

namespace MessageBroker.Core.Tests.Configuration;

public class ConfigurationDiffEngineTests
{
    [Fact]
    public void CalculateDiff_BothNull_ReturnsEmptyDiff()
    {
        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(null, null);

        // Assert
        Assert.NotNull(diff);
        Assert.Empty(diff.Changes);
    }

    [Fact]
    public void CalculateDiff_OldConfigNull_ReturnsConfigurationChange()
    {
        // Arrange
        var newConfig = new BrokerConfiguration();

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(null, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("Configuration", diff.Changes[0].PropertyName);
        Assert.Null(diff.Changes[0].OldValue);
        Assert.NotNull(diff.Changes[0].NewValue);
    }

    [Fact]
    public void CalculateDiff_NewConfigNull_ReturnsConfigurationChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration();

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, null);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("Configuration", diff.Changes[0].PropertyName);
        Assert.NotNull(diff.Changes[0].OldValue);
        Assert.Null(diff.Changes[0].NewValue);
    }

    [Fact]
    public void CalculateDiff_IdenticalConfigs_ReturnsEmptyDiff()
    {
        // Arrange
        var config1 = new BrokerConfiguration
        {
            Port = 4222,
            Host = "localhost",
            Debug = false
        };
        var config2 = new BrokerConfiguration
        {
            Port = 4222,
            Host = "localhost",
            Debug = false
        };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(config1, config2);

        // Assert
        Assert.Empty(diff.Changes);
    }

    [Fact]
    public void CalculateDiff_PortChange_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration { Port = 4222 };
        var newConfig = new BrokerConfiguration { Port = 5000 };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("Port", diff.Changes[0].PropertyName);
        Assert.Equal(4222, diff.Changes[0].OldValue);
        Assert.Equal(5000, diff.Changes[0].NewValue);
    }

    [Fact]
    public void CalculateDiff_HostChange_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration { Host = "localhost" };
        var newConfig = new BrokerConfiguration { Host = "0.0.0.0" };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("Host", diff.Changes[0].PropertyName);
        Assert.Equal("localhost", diff.Changes[0].OldValue);
        Assert.Equal("0.0.0.0", diff.Changes[0].NewValue);
    }

    [Fact]
    public void CalculateDiff_BooleanChange_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration { Debug = false };
        var newConfig = new BrokerConfiguration { Debug = true };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("Debug", diff.Changes[0].PropertyName);
        Assert.Equal(false, diff.Changes[0].OldValue);
        Assert.Equal(true, diff.Changes[0].NewValue);
    }

    [Fact]
    public void CalculateDiff_MultipleChanges_DetectsAll()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration
        {
            Port = 4222,
            Debug = false,
            MaxPayload = 1048576
        };
        var newConfig = new BrokerConfiguration
        {
            Port = 5000,
            Debug = true,
            MaxPayload = 2097152
        };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Equal(3, diff.Changes.Count);
        Assert.Contains(diff.Changes, c => c.PropertyName == "Port");
        Assert.Contains(diff.Changes, c => c.PropertyName == "Debug");
        Assert.Contains(diff.Changes, c => c.PropertyName == "MaxPayload");
    }

    [Fact]
    public void CalculateDiff_AuthUsernameChange_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration
        {
            Auth = new AuthConfiguration { Username = "user1", Password = "pass" }
        };
        var newConfig = new BrokerConfiguration
        {
            Auth = new AuthConfiguration { Username = "user2", Password = "pass" }
        };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("Auth.Username", diff.Changes[0].PropertyName);
        Assert.Equal("user1", diff.Changes[0].OldValue);
        Assert.Equal("user2", diff.Changes[0].NewValue);
    }

    [Fact]
    public void CalculateDiff_LeafNodePortChange_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration { Port = 7422 }
        };
        var newConfig = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration { Port = 7423 }
        };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("LeafNode.Port", diff.Changes[0].PropertyName);
        Assert.Equal(7422, diff.Changes[0].OldValue);
        Assert.Equal(7423, diff.Changes[0].NewValue);
    }

    [Fact]
    public void CalculateDiff_RemoteUrlsChange_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                RemoteUrls = new List<string> { "nats://server1:7422" }
            }
        };
        var newConfig = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                RemoteUrls = new List<string> { "nats://server2:7422" }
            }
        };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("LeafNode.RemoteUrls", diff.Changes[0].PropertyName);
    }

    [Fact]
    public void CalculateDiff_RemoteUrlsAdded_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                RemoteUrls = new List<string> { "nats://server1:7422" }
            }
        };
        var newConfig = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                RemoteUrls = new List<string> { "nats://server1:7422", "nats://server2:7422" }
            }
        };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("LeafNode.RemoteUrls", diff.Changes[0].PropertyName);
    }

    [Fact]
    public void CalculateDiff_IgnoresConfigurationIdAndCreatedAt()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration();
        var newConfig = new BrokerConfiguration
        {
            ConfigurationId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Empty(diff.Changes);
    }

    [Fact]
    public void CalculateDiff_DescriptionChange_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration { Description = "Old description" };
        var newConfig = new BrokerConfiguration { Description = "New description" };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("Description", diff.Changes[0].PropertyName);
        Assert.Equal("Old description", diff.Changes[0].OldValue);
        Assert.Equal("New description", diff.Changes[0].NewValue);
    }

    [Fact]
    public void CalculateDiff_AuthAllowedUsersChange_DetectsChange()
    {
        // Arrange
        var oldConfig = new BrokerConfiguration
        {
            Auth = new AuthConfiguration
            {
                AllowedUsers = new List<string> { "user1" }
            }
        };
        var newConfig = new BrokerConfiguration
        {
            Auth = new AuthConfiguration
            {
                AllowedUsers = new List<string> { "user1", "user2" }
            }
        };

        // Act
        var diff = ConfigurationDiffEngine.CalculateDiff(oldConfig, newConfig);

        // Assert
        Assert.Single(diff.Changes);
        Assert.Equal("Auth.AllowedUsers", diff.Changes[0].PropertyName);
    }
}
