using MessageBroker.Core.Configuration;
using Xunit;

namespace MessageBroker.Core.Tests.Configuration;

public class InMemoryConfigurationStoreTests
{
    [Fact]
    public async Task SaveAsync_AssignsVersionNumber()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        var version = new ConfigurationVersion
        {
            Configuration = new BrokerConfiguration(),
            Description = "Test version"
        };

        // Act
        await store.SaveAsync(version);

        // Assert
        Assert.Equal(1, version.Version);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task SaveAsync_IncrementingVersionNumbers()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        var version1 = new ConfigurationVersion { Configuration = new BrokerConfiguration() };
        var version2 = new ConfigurationVersion { Configuration = new BrokerConfiguration() };
        var version3 = new ConfigurationVersion { Configuration = new BrokerConfiguration() };

        // Act
        await store.SaveAsync(version1);
        await store.SaveAsync(version2);
        await store.SaveAsync(version3);

        // Assert
        Assert.Equal(1, version1.Version);
        Assert.Equal(2, version2.Version);
        Assert.Equal(3, version3.Version);
        Assert.Equal(3, store.Count);
    }

    [Fact]
    public async Task SaveAsync_NullVersion_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!));
    }

    [Fact]
    public async Task GetVersionAsync_ExistingVersion_ReturnsVersion()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        var version = new ConfigurationVersion
        {
            Configuration = new BrokerConfiguration { Port = 4222 },
            Description = "Test"
        };
        await store.SaveAsync(version);

        // Act
        var retrieved = await store.GetVersionAsync(1);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved.Version);
        Assert.Equal("Test", retrieved.Description);
        Assert.Equal(4222, retrieved.Configuration.Port);
    }

    [Fact]
    public async Task GetVersionAsync_NonExistingVersion_ReturnsNull()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();

        // Act
        var retrieved = await store.GetVersionAsync(999);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetLatestAsync_EmptyStore_ReturnsNull()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();

        // Act
        var latest = await store.GetLatestAsync();

        // Assert
        Assert.Null(latest);
    }

    [Fact]
    public async Task GetLatestAsync_WithVersions_ReturnsNewest()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        var version1 = new ConfigurationVersion { Configuration = new BrokerConfiguration(), Description = "V1" };
        var version2 = new ConfigurationVersion { Configuration = new BrokerConfiguration(), Description = "V2" };
        var version3 = new ConfigurationVersion { Configuration = new BrokerConfiguration(), Description = "V3" };

        await store.SaveAsync(version1);
        await store.SaveAsync(version2);
        await store.SaveAsync(version3);

        // Act
        var latest = await store.GetLatestAsync();

        // Assert
        Assert.NotNull(latest);
        Assert.Equal(3, latest.Version);
        Assert.Equal("V3", latest.Description);
    }

    [Fact]
    public async Task GetHistoryAsync_EmptyStore_ReturnsEmptyList()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();

        // Act
        var history = await store.GetHistoryAsync();

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetHistoryAsync_WithVersions_ReturnsInDescendingOrder()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        for (int i = 1; i <= 5; i++)
        {
            await store.SaveAsync(new ConfigurationVersion
            {
                Configuration = new BrokerConfiguration(),
                Description = $"V{i}"
            });
        }

        // Act
        var history = await store.GetHistoryAsync(count: 3);

        // Assert
        Assert.Equal(3, history.Count);
        Assert.Equal(5, history[0].Version);
        Assert.Equal(4, history[1].Version);
        Assert.Equal(3, history[2].Version);
    }

    [Fact]
    public async Task GetHistoryAsync_RequestMoreThanAvailable_ReturnsAll()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });

        // Act
        var history = await store.GetHistoryAsync(count: 10);

        // Assert
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task Clear_RemovesAllVersions()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });

        // Act
        store.Clear();

        // Assert
        Assert.Equal(0, store.Count);
        var latest = await store.GetLatestAsync();
        Assert.Null(latest);
    }

    [Fact]
    public async Task Clear_ResetsVersionNumbering()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });
        store.Clear();

        // Act
        var version = new ConfigurationVersion { Configuration = new BrokerConfiguration() };
        await store.SaveAsync(version);

        // Assert
        Assert.Equal(1, version.Version);
    }

    [Fact]
    public async Task GetAll_ReturnsVersionsInAscendingOrder()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration(), Description = "V1" });
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration(), Description = "V2" });
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration(), Description = "V3" });

        // Act
        var all = store.GetAll();

        // Assert
        Assert.Equal(3, all.Count);
        Assert.Equal(1, all[0].Version);
        Assert.Equal(2, all[1].Version);
        Assert.Equal(3, all[2].Version);
    }

    [Fact]
    public async Task SaveAsync_WithManualVersionNumber_UpdatesNextVersion()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        var version = new ConfigurationVersion
        {
            Version = 5,
            Configuration = new BrokerConfiguration()
        };

        // Act
        await store.SaveAsync(version);
        var nextVersion = new ConfigurationVersion { Configuration = new BrokerConfiguration() };
        await store.SaveAsync(nextVersion);

        // Assert
        Assert.Equal(5, version.Version);
        Assert.Equal(6, nextVersion.Version);
    }

    [Fact]
    public async Task Count_ReflectsNumberOfVersions()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();

        // Act & Assert
        Assert.Equal(0, store.Count);

        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });
        Assert.Equal(1, store.Count);

        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() });
        Assert.Equal(2, store.Count);

        store.Clear();
        Assert.Equal(0, store.Count);
    }
}
