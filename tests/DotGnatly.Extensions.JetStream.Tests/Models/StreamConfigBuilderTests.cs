using DotGnatly.Extensions.JetStream.Models;
using NATS.Client.JetStream.Models;
using Xunit;

namespace DotGnatly.Extensions.JetStream.Tests.Models;

public class StreamConfigBuilderTests
{
    [Fact]
    public void Constructor_WithValidName_SetsName()
    {
        // Arrange & Act
        var builder = new StreamConfigBuilder("TEST_STREAM");
        var config = builder.WithSubjects("test.*").Build();

        // Assert
        Assert.Equal("TEST_STREAM", config.Name);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StreamConfigBuilder(null!));
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StreamConfigBuilder(""));
    }

    [Fact]
    public void WithSubjects_AddsSubjects()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("orders.*", "shipments.*");
        var config = builder.Build();

        // Assert
        Assert.NotNull(config.Subjects);
        Assert.NotEmpty(config.Subjects);
        Assert.Equal(2, config.Subjects.Count);
        Assert.Contains("orders.*", config.Subjects);
        Assert.Contains("shipments.*", config.Subjects);
    }

    [Fact]
    public void WithSubjects_NullSubjects_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithSubjects(null!));
    }

    [Fact]
    public void WithRetention_SetsRetention()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithRetention(StreamConfigRetention.Workqueue);
        var config = builder.Build();

        // Assert
        Assert.Equal(StreamConfigRetention.Workqueue, config.Retention);
    }

    [Fact]
    public void WithStorage_SetsStorage()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithStorage(StreamConfigStorage.Memory);
        var config = builder.Build();

        // Assert
        Assert.Equal(StreamConfigStorage.Memory, config.Storage);
    }

    [Fact]
    public void WithReplicas_ValidValue_SetsReplicas()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithReplicas(3);
        var config = builder.Build();

        // Assert
        Assert.Equal(3, config.NumReplicas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void WithReplicas_InvalidValue_ThrowsArgumentOutOfRangeException(int replicas)
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.WithReplicas(replicas));
    }

    [Fact]
    public void WithMaxMessages_SetsMaxMessages()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithMaxMessages(10000);
        var config = builder.Build();

        // Assert
        Assert.Equal(10000, config.MaxMsgs);
    }

    [Fact]
    public void WithMaxBytes_SetsMaxBytes()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithMaxBytes(1024 * 1024);
        var config = builder.Build();

        // Assert
        Assert.Equal(1024 * 1024, config.MaxBytes);
    }

    [Fact]
    public void WithMaxAge_SetsMaxAge()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");
        var maxAge = TimeSpan.FromDays(7);

        // Act
        builder.WithSubjects("test.*").WithMaxAge(maxAge);
        var config = builder.Build();

        // Assert
        Assert.Equal(maxAge, config.MaxAge);
    }

    [Fact]
    public void WithMaxMessageSize_SetsMaxMessageSize()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithMaxMessageSize(1024);
        var config = builder.Build();

        // Assert
        Assert.Equal(1024, config.MaxMsgSize);
    }

    [Fact]
    public void WithMaxConsumers_SetsMaxConsumers()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithMaxConsumers(10);
        var config = builder.Build();

        // Assert
        Assert.Equal(10, config.MaxConsumers);
    }

    [Fact]
    public void WithDiscard_SetsDiscard()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithDiscard(StreamConfigDiscard.New);
        var config = builder.Build();

        // Assert
        Assert.Equal(StreamConfigDiscard.New, config.Discard);
    }

    [Fact]
    public void WithNoAck_SetsNoAck()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithNoAck();
        var config = builder.Build();

        // Assert
        Assert.True(config.NoAck);
    }

    [Fact]
    public void WithDescription_SetsDescription()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithDescription("Test stream");
        var config = builder.Build();

        // Assert
        Assert.Equal("Test stream", config.Description);
    }

    [Fact]
    public void WithDuplicateWindow_SetsDuplicateWindow()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");
        var window = TimeSpan.FromMinutes(5);

        // Act
        builder.WithSubjects("test.*").WithDuplicateWindow(window);
        var config = builder.Build();

        // Assert
        Assert.Equal(window, config.DuplicateWindow);
    }

    [Fact]
    public void WithMaxMessagesPerSubject_SetsMaxMessagesPerSubject()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithMaxMessagesPerSubject(1);
        var config = builder.Build();

        // Assert
        Assert.Equal(1, config.MaxMsgsPerSubject);
    }

    [Fact]
    public void WithPlacement_SetsPlacement()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithPlacement("cluster-1", new[] { "tag1", "tag2" });
        var config = builder.Build();

        // Assert
        Assert.NotNull(config.Placement);
        Assert.Equal("cluster-1", config.Placement!.Cluster);
        Assert.NotNull(config.Placement.Tags);
        Assert.NotEmpty(config.Placement.Tags);
        Assert.Equal(2, config.Placement.Tags.Count);
        Assert.Contains("tag1", config.Placement.Tags);
        Assert.Contains("tag2", config.Placement.Tags);
    }

    [Fact]
    public void AddSource_AddsSource()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*")
               .AddSource("SOURCE1", "source.*");
        var config = builder.Build();

        // Assert
        Assert.NotNull(config.Sources);
        Assert.Single(config.Sources);
        Assert.Equal("SOURCE1", config.Sources.First().Name);
        Assert.Equal("source.*", config.Sources.First().FilterSubject);
    }

    [Fact]
    public void AddSource_WithExternal_AddsSourceWithExternal()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*")
               .AddSource("SOURCE1", "source.*", "$JS.hub.API", "deliver.hub");
        var config = builder.Build();

        // Assert
        Assert.NotNull(config.Sources);
        Assert.Single(config.Sources);
        Assert.NotNull(config.Sources.First().External);
        Assert.Equal("$JS.hub.API", config.Sources.First().External?.Api);
        Assert.Equal("deliver.hub", config.Sources.First().External?.Deliver);
    }

    [Fact]
    public void WithSealed_SetsSealed()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithSealed();
        var config = builder.Build();

        // Assert
        Assert.True(config.Sealed);
    }

    [Fact]
    public void WithDenyDelete_SetsDenyDelete()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithDenyDelete();
        var config = builder.Build();

        // Assert
        Assert.True(config.DenyDelete);
    }

    [Fact]
    public void WithDenyPurge_SetsDenyPurge()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithDenyPurge();
        var config = builder.Build();

        // Assert
        Assert.True(config.DenyPurge);
    }

    [Fact]
    public void WithAllowRollup_SetsAllowRollup()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithAllowRollup();
        var config = builder.Build();

        // Assert
        Assert.True(config.AllowRollupHdrs);
    }

    [Fact]
    public void WithAllowDirect_SetsAllowDirect()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act
        builder.WithSubjects("test.*").WithAllowDirect();
        var config = builder.Build();

        // Assert
        Assert.True(config.AllowDirect);
    }

    [Fact]
    public void Build_NoSubjects_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new StreamConfigBuilder("TEST");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void FluentAPI_ChainMultipleMethods_BuildsCorrectConfig()
    {
        // Arrange
        var builder = new StreamConfigBuilder("ORDERS");

        // Act
        var config = builder
            .WithSubjects("orders.*", "shipments.*")
            .WithDescription("Order processing stream")
            .WithStorage(StreamConfigStorage.File)
            .WithRetention(StreamConfigRetention.Limits)
            .WithReplicas(3)
            .WithMaxMessages(100000)
            .WithMaxBytes(1024 * 1024 * 1024)
            .WithMaxAge(TimeSpan.FromDays(30))
            .WithMaxMessageSize(1024 * 1024)
            .WithMaxConsumers(10)
            .WithDiscard(StreamConfigDiscard.Old)
            .WithDuplicateWindow(TimeSpan.FromMinutes(2))
            .WithMaxMessagesPerSubject(1000)
            .WithPlacement("cluster-1")
            .WithAllowDirect()
            .Build();

        // Assert
        Assert.Equal("ORDERS", config.Name);
        Assert.NotNull(config.Subjects);
        Assert.NotEmpty(config.Subjects);
        Assert.Equal(2, config.Subjects.Count);
        Assert.Equal("Order processing stream", config.Description);
        Assert.Equal(StreamConfigStorage.File, config.Storage);
        Assert.Equal(StreamConfigRetention.Limits, config.Retention);
        Assert.Equal(3, config.NumReplicas);
        Assert.Equal(100000, config.MaxMsgs);
        Assert.Equal(1024 * 1024 * 1024, config.MaxBytes);
        Assert.Equal(TimeSpan.FromDays(30), config.MaxAge);
        Assert.Equal(1024 * 1024, config.MaxMsgSize);
        Assert.Equal(10, config.MaxConsumers);
        Assert.Equal(StreamConfigDiscard.Old, config.Discard);
        Assert.Equal(TimeSpan.FromMinutes(2), config.DuplicateWindow);
        Assert.Equal(1000, config.MaxMsgsPerSubject);
        Assert.NotNull(config.Placement);
        Assert.Equal("cluster-1", config.Placement!.Cluster);
        Assert.True(config.AllowDirect);
    }
}
