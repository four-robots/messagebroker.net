using DotGnatly.Extensions.JetStream.Models;
using NATS.Client.KeyValueStore;
using Xunit;

namespace DotGnatly.Extensions.JetStream.Tests.Models;

public class KVConfigBuilderTests
{
    [Fact]
    public void Constructor_WithValidBucket_SetsBucket()
    {
        // Arrange & Act
        var builder = new KVConfigBuilder("TEST_BUCKET");
        var config = builder.Build();

        // Assert
        Assert.Equal("TEST_BUCKET", config.Bucket);
    }

    [Fact]
    public void Constructor_WithNullBucket_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KVConfigBuilder(null!));
    }

    [Fact]
    public void Constructor_WithEmptyBucket_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new KVConfigBuilder(""));
    }

    [Fact]
    public void WithDescription_SetsDescription()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        builder.WithDescription("Test KV store");
        var config = builder.Build();

        // Assert
        Assert.Equal("Test KV store", config.Description);
    }

    [Fact]
    public void WithMaxHistoryPerKey_SetsMaxHistory()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        builder.WithMaxHistoryPerKey(10);
        var config = builder.Build();

        // Assert
        Assert.Equal(10, config.History);
    }

    [Fact]
    public void WithTTL_SetsTTL()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");
        var ttl = TimeSpan.FromHours(24);

        // Act
        builder.WithTTL(ttl);
        var config = builder.Build();

        // Assert
        Assert.Equal(ttl, config.Ttl);
    }

    [Fact]
    public void WithMaxBucketSize_SetsMaxBytes()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        builder.WithMaxBucketSize(1024 * 1024);
        var config = builder.Build();

        // Assert
        Assert.Equal(1024 * 1024, config.MaxBytes);
    }

    [Fact]
    public void WithReplicas_ValidValue_SetsReplicas()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        builder.WithReplicas(3);
        var config = builder.Build();

        // Assert
        Assert.Equal(3, config.NumberOfReplicas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void WithReplicas_InvalidValue_ThrowsArgumentOutOfRangeException(int replicas)
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithReplicas(replicas));
    }

    [Fact]
    public void WithStorage_SetsStorage()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        builder.WithStorage(NatsKVStorageType.Memory);
        var config = builder.Build();

        // Assert
        Assert.Equal(NatsKVStorageType.Memory, config.Storage);
    }

    [Fact]
    public void Build_DefaultValues_CreatesValidConfig()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        var config = builder.Build();

        // Assert
        Assert.Equal("TEST", config.Bucket);
        Assert.Null(config.Description);
        Assert.Equal(1, config.History);
        Assert.Equal(TimeSpan.Zero, config.Ttl);
        Assert.Equal(-1, config.MaxBytes);
        Assert.Equal(1, config.NumberOfReplicas);
        Assert.Equal(NatsKVStorageType.File, config.Storage);
    }

    [Fact]
    public void FluentAPI_ChainMultipleMethods_BuildsCorrectConfig()
    {
        // Arrange
        var builder = new KVConfigBuilder("USER_SESSIONS");
        var ttl = TimeSpan.FromHours(2);

        // Act
        var config = builder
            .WithDescription("User session storage")
            .WithMaxHistoryPerKey(5)
            .WithTTL(ttl)
            .WithMaxBucketSize(1024 * 1024 * 1024)
            .WithReplicas(3)
            .WithStorage(NatsKVStorageType.Memory)
            .Build();

        // Assert
        Assert.Equal("USER_SESSIONS", config.Bucket);
        Assert.Equal("User session storage", config.Description);
        Assert.Equal(5, config.History);
        Assert.Equal(ttl, config.Ttl);
        Assert.Equal(1024 * 1024 * 1024, config.MaxBytes);
        Assert.Equal(3, config.NumberOfReplicas);
        Assert.Equal(NatsKVStorageType.Memory, config.Storage);
    }

    [Fact]
    public void Build_MultipleBuilds_CreatesIndependentConfigs()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        var config1 = builder.WithDescription("First").Build();
        var config2 = builder.WithDescription("Second").Build();

        // Assert
        Assert.Equal("First", config1.Description);
        Assert.Equal("Second", config2.Description);
    }

    [Fact]
    public void WithTTL_ZeroValue_NoExpiration()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        builder.WithTTL(TimeSpan.Zero);
        var config = builder.Build();

        // Assert
        Assert.Equal(TimeSpan.Zero, config.Ttl);
    }

    [Fact]
    public void WithMaxBucketSize_NegativeValue_Unlimited()
    {
        // Arrange
        var builder = new KVConfigBuilder("TEST");

        // Act
        builder.WithMaxBucketSize(-1);
        var config = builder.Build();

        // Assert
        Assert.Equal(-1, config.MaxBytes);
    }
}
