using DotGnatly.Core.Configuration;
using Xunit;

namespace DotGnatly.Core.Tests.Configuration;

public class InMemoryConfigurationStoreTests
{
    [Fact]
    public async Task SaveAsync_AssignsVersionNumber()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        var version = new ConfigurationVersion
        {
            Configuration = new BrokerConfiguration { Description = "Test version" }
        };

        // Act
        await store.SaveAsync(version, TestContext.Current.CancellationToken);

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
        await store.SaveAsync(version1, TestContext.Current.CancellationToken);
        await store.SaveAsync(version2, TestContext.Current.CancellationToken);
        await store.SaveAsync(version3, TestContext.Current.CancellationToken);

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
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetVersionAsync_ExistingVersion_ReturnsVersion()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        var version = new ConfigurationVersion
        {
            Configuration = new BrokerConfiguration { Port = 4222, Description = "Test" }
        };
        await store.SaveAsync(version, TestContext.Current.CancellationToken);

        // Act
        var retrieved = await store.GetVersionAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved.Version);
        Assert.Equal("Test", retrieved.Configuration.Description);
        Assert.Equal(4222, retrieved.Configuration.Port);
    }

    [Fact]
    public async Task GetVersionAsync_NonExistingVersion_ReturnsNull()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();

        // Act
        var retrieved = await store.GetVersionAsync(999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetLatestAsync_EmptyStore_ReturnsNull()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();

        // Act
        var latest = await store.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(latest);
    }

    [Fact]
    public async Task GetLatestAsync_WithVersions_ReturnsNewest()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        var version1 = new ConfigurationVersion { Configuration = new BrokerConfiguration { Description = "V1" } };
        var version2 = new ConfigurationVersion { Configuration = new BrokerConfiguration { Description = "V2" } };
        var version3 = new ConfigurationVersion { Configuration = new BrokerConfiguration { Description = "V3" } };

        await store.SaveAsync(version1, TestContext.Current.CancellationToken);
        await store.SaveAsync(version2, TestContext.Current.CancellationToken);
        await store.SaveAsync(version3, TestContext.Current.CancellationToken);

        // Act
        var latest = await store.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(latest);
        Assert.Equal(3, latest.Version);
        Assert.Equal("V3", latest.Configuration.Description);
    }

    [Fact]
    public async Task GetHistoryAsync_EmptyStore_ReturnsEmptyList()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();

        // Act
        var history = await store.GetHistoryAsync(cancellationToken: TestContext.Current.CancellationToken);

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
                Configuration = new BrokerConfiguration { Description = $"V{i}" }
            }, TestContext.Current.CancellationToken);
        }

        // Act
        var history = await store.GetHistoryAsync(count: 3, cancellationToken: TestContext.Current.CancellationToken);

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
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);

        // Act
        var history = await store.GetHistoryAsync(count: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task Clear_RemovesAllVersions()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);

        // Act
        store.Clear();

        // Assert
        Assert.Equal(0, store.Count);
        var latest = await store.GetLatestAsync(TestContext.Current.CancellationToken);
        Assert.Null(latest);
    }

    [Fact]
    public async Task Clear_ResetsVersionNumbering()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);
        store.Clear();

        // Act
        var version = new ConfigurationVersion { Configuration = new BrokerConfiguration() };
        await store.SaveAsync(version, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, version.Version);
    }

    [Fact]
    public async Task GetAll_ReturnsVersionsInAscendingOrder()
    {
        // Arrange
        var store = new InMemoryConfigurationStore();
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration { Description = "V1" } }, TestContext.Current.CancellationToken);
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration { Description = "V2" } }, TestContext.Current.CancellationToken);
        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration { Description = "V3" } }, TestContext.Current.CancellationToken);

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
        await store.SaveAsync(version, TestContext.Current.CancellationToken);
        var nextVersion = new ConfigurationVersion { Configuration = new BrokerConfiguration() };
        await store.SaveAsync(nextVersion, TestContext.Current.CancellationToken);

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

        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);
        Assert.Equal(1, store.Count);

        await store.SaveAsync(new ConfigurationVersion { Configuration = new BrokerConfiguration() }, TestContext.Current.CancellationToken);
        Assert.Equal(2, store.Count);

        store.Clear();
        Assert.Equal(0, store.Count);
    }
}
