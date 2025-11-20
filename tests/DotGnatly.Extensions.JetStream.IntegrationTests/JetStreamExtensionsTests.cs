using DotGnatly.Core.Configuration;
using DotGnatly.Extensions.JetStream.Extensions;
using DotGnatly.Extensions.JetStream.Models;
using DotGnatly.Nats.Implementation;
using NATS.Client.JetStream.Models;
using Xunit;

namespace DotGnatly.Extensions.JetStream.IntegrationTests;

public class JetStreamExtensionsTests : IAsyncLifetime
{
    private NatsController? _controller;
    private readonly string _jetstreamDir;

    public JetStreamExtensionsTests()
    {
        _jetstreamDir = Path.Combine(Path.GetTempPath(), $"jetstream-tests-{Guid.NewGuid()}");
    }

    public async ValueTask InitializeAsync()
    {
        _controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Port = 4222,
            Host = "localhost",
            Jetstream = true,
            JetstreamStoreDir = _jetstreamDir,
            Debug = false
        };

        var result = await _controller.ConfigureAsync(config);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await _controller.WaitForReadyAsync(timeoutSeconds: 10);
    }

    public async ValueTask DisposeAsync()
    {
        if (_controller != null)
        {
            await _controller.ShutdownAsync();
            _controller.Dispose();
        }

        if (Directory.Exists(_jetstreamDir))
        {
            try
            {
                Directory.Delete(_jetstreamDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task CreateStreamAsync_BasicStream_CreatesSuccessfully()
    {
        // Arrange & Act
        var streamInfo = await _controller!.CreateStreamAsync("TEST_BASIC", builder =>
        {
            builder
                .WithSubjects("test.basic.*")
                .WithDescription("Basic test stream")
                .WithStorage(StreamConfigStorage.File)
                .WithMaxMessages(1000);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_BASIC", streamInfo.Config.Name);
        Assert.NotNull(streamInfo.Config.Subjects);
        Assert.NotEmpty(streamInfo.Config.Subjects);
        Assert.Contains("test.basic.*", streamInfo.Config.Subjects);
        Assert.Equal("Basic test stream", streamInfo.Config.Description);
        Assert.Equal(StreamConfigStorage.File, streamInfo.Config.Storage);
        Assert.Equal(1000, streamInfo.Config.MaxMsgs);
    }

    [Fact]
    public async Task CreateStreamAsync_MemoryStream_CreatesSuccessfully()
    {
        // Arrange & Act
        var streamInfo = await _controller!.CreateStreamAsync("TEST_MEMORY", builder =>
        {
            builder
                .WithSubjects("test.memory.*")
                .WithStorage(StreamConfigStorage.Memory)
                .WithMaxAge(TimeSpan.FromHours(1));
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_MEMORY", streamInfo.Config.Name);
        Assert.Equal(StreamConfigStorage.Memory, streamInfo.Config.Storage);
        Assert.Equal(TimeSpan.FromHours(1), streamInfo.Config.MaxAge);
    }

    [Fact]
    public async Task CreateStreamAsync_WorkQueueStream_CreatesSuccessfully()
    {
        // Arrange & Act
        var streamInfo = await _controller!.CreateStreamAsync("TEST_WORKQUEUE", builder =>
        {
            builder
                .WithSubjects("test.workqueue.*")
                .WithRetention(StreamConfigRetention.Workqueue)
                .WithMaxMessages(5000);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_WORKQUEUE", streamInfo.Config.Name);
        Assert.Equal(StreamConfigRetention.Workqueue, streamInfo.Config.Retention);
        Assert.Equal(5000, streamInfo.Config.MaxMsgs);
    }

    [Fact]
    public async Task CreateStreamAsync_WithAdvancedFeatures_CreatesSuccessfully()
    {
        // Arrange & Act
        var streamInfo = await _controller!.CreateStreamAsync("TEST_ADVANCED", builder =>
        {
            builder
                .WithSubjects("test.advanced.*")
                .WithMaxMessagesPerSubject(1)
                .WithDenyDelete()
                .WithDenyPurge()
                .WithAllowRollup()
                .WithAllowDirect();
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_ADVANCED", streamInfo.Config.Name);
        Assert.Equal(1, streamInfo.Config.MaxMsgsPerSubject);
        Assert.True(streamInfo.Config.DenyDelete);
        Assert.True(streamInfo.Config.DenyPurge);
        Assert.True(streamInfo.Config.AllowRollupHdrs);
        Assert.True(streamInfo.Config.AllowDirect);
    }

    [Fact]
    public async Task ListStreamsAsync_ReturnsAllStreams()
    {
        // Arrange
        await _controller!.CreateStreamAsync("LIST_TEST_1", b => b.WithSubjects("list.1.*"), cancellationToken: TestContext.Current.CancellationToken);
        await _controller!.CreateStreamAsync("LIST_TEST_2", b => b.WithSubjects("list.2.*"), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var streams = await _controller!.ListStreamsAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("LIST_TEST_1", streams);
        Assert.Contains("LIST_TEST_2", streams);
    }

    [Fact]
    public async Task GetStreamAsync_ExistingStream_ReturnsStreamInfo()
    {
        // Arrange
        await _controller!.CreateStreamAsync("GET_TEST", b => b.WithSubjects("get.test.*"), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var streamInfo = await _controller!.GetStreamAsync("GET_TEST", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("GET_TEST", streamInfo.Config.Name);
        Assert.NotNull(streamInfo.Config.Subjects);
        Assert.NotEmpty(streamInfo.Config.Subjects);
        Assert.Contains("get.test.*", streamInfo.Config.Subjects);
    }

    [Fact]
    public async Task UpdateStreamAsync_UpdatesExistingStream()
    {
        // Arrange
        await _controller!.CreateStreamAsync("UPDATE_TEST", b => b
            .WithSubjects("update.test.*")
            .WithMaxMessages(1000), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var updatedInfo = await _controller!.UpdateStreamAsync("UPDATE_TEST", b => b
            .WithSubjects("update.test.*")
            .WithMaxMessages(2000)
            .WithDescription("Updated stream"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("UPDATE_TEST", updatedInfo.Config.Name);
        Assert.Equal(2000, updatedInfo.Config.MaxMsgs);
        Assert.Equal("Updated stream", updatedInfo.Config.Description);
    }

    [Fact]
    public async Task DeleteStreamAsync_DeletesStream()
    {
        // Arrange
        await _controller!.CreateStreamAsync("DELETE_TEST", b => b.WithSubjects("delete.test.*"), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var deleted = await _controller!.DeleteStreamAsync("DELETE_TEST", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(deleted);
        var streams = await _controller!.ListStreamsAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.DoesNotContain("DELETE_TEST", streams);
    }

    [Fact]
    public async Task PurgeStreamAsync_PurgesMessages()
    {
        // Arrange
        await _controller!.CreateStreamAsync("PURGE_TEST", b => b.WithSubjects("purge.test.*"), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var purgeResponse = await _controller!.PurgeStreamAsync("PURGE_TEST", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(purgeResponse);
        Assert.True(purgeResponse.Success);
    }

    [Fact]
    public async Task CreateStreamFromJsonAsync_CreatesFromJsonString()
    {
        // Arrange
        var json = @"{
            ""name"": ""JSON_TEST"",
            ""subjects"": [""json.test.*""],
            ""retention"": ""limits"",
            ""max_msgs"": 5000,
            ""storage"": ""file""
        }";

        // Act
        var streamInfo = await _controller!.CreateStreamFromJsonAsync(json, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("JSON_TEST", streamInfo.Config.Name);
        Assert.NotNull(streamInfo.Config.Subjects);
        Assert.NotEmpty(streamInfo.Config.Subjects);
        Assert.Contains("json.test.*", streamInfo.Config.Subjects);
        Assert.Equal(5000, streamInfo.Config.MaxMsgs);
    }

    [Fact]
    public async Task CreateStreamFromFileAsync_CreatesFromJsonFile()
    {
        // Arrange
        var json = @"{
            ""name"": ""FILE_TEST"",
            ""subjects"": [""file.test.*""],
            ""retention"": ""limits"",
            ""storage"": ""file"",
            ""max_msgs"": 3000
        }";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, json, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var streamInfo = await _controller!.CreateStreamFromFileAsync(tempFile, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("FILE_TEST", streamInfo.Config.Name);
            Assert.NotNull(streamInfo.Config.Subjects);
            Assert.NotEmpty(streamInfo.Config.Subjects);
            Assert.Contains("file.test.*", streamInfo.Config.Subjects);
            Assert.Equal(3000, streamInfo.Config.MaxMsgs);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task CreateStreamsFromDirectoryAsync_CreatesMultipleStreams()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var json1 = @"{""name"": ""DIR_TEST_1"", ""subjects"": [""dir.1.*""]}";
        var json2 = @"{""name"": ""DIR_TEST_2"", ""subjects"": [""dir.2.*""]}";

        await File.WriteAllTextAsync(Path.Combine(tempDir, "stream1.json"), json1, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "stream2.json"), json2, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var streamInfos = await _controller!.CreateStreamsFromDirectoryAsync(tempDir, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, streamInfos.Count);
            Assert.Contains(streamInfos, s => s.Config.Name == "DIR_TEST_1");
            Assert.Contains(streamInfos, s => s.Config.Name == "DIR_TEST_2");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task UpdateStreamFromFileAsync_UpdatesFromJsonFile()
    {
        // Arrange
        await _controller!.CreateStreamAsync("UPDATE_FILE_TEST", b => b
            .WithSubjects("update.file.*")
            .WithMaxMessages(1000), cancellationToken: TestContext.Current.CancellationToken);

        var json = @"{
            ""name"": ""UPDATE_FILE_TEST"",
            ""subjects"": [""update.file.*""],
            ""max_msgs"": 5000,
            ""description"": ""Updated from file""
        }";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, json, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var streamInfo = await _controller!.UpdateStreamFromFileAsync(tempFile, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("UPDATE_FILE_TEST", streamInfo.Config.Name);
            Assert.Equal(5000, streamInfo.Config.MaxMsgs);
            Assert.Equal("Updated from file", streamInfo.Config.Description);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task GetJetStreamContextAsync_ReturnsValidContext()
    {
        // Act
        await using var context = await _controller!.GetJetStreamContextAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.JetStream);
        Assert.NotNull(context.Connection);
    }

    [Fact]
    public async Task CreateStreamAsync_WithMultipleSubjects_CreatesSuccessfully()
    {
        // Arrange & Act
        var streamInfo = await _controller!.CreateStreamAsync("MULTI_SUBJECT_TEST", builder =>
        {
            builder
                .WithSubjects("orders.*", "shipments.*", "inventory.*")
                .WithMaxMessages(10000);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("MULTI_SUBJECT_TEST", streamInfo.Config.Name);
        Assert.NotNull(streamInfo.Config.Subjects);
        Assert.NotEmpty(streamInfo.Config.Subjects);
        Assert.Equal(3, streamInfo.Config.Subjects.Count);
        Assert.Contains("orders.*", streamInfo.Config.Subjects);
        Assert.Contains("shipments.*", streamInfo.Config.Subjects);
        Assert.Contains("inventory.*", streamInfo.Config.Subjects);
    }

    [Fact]
    public async Task CreateStreamAsync_WithComplexConfiguration_CreatesSuccessfully()
    {
        // Arrange & Act
        var streamInfo = await _controller!.CreateStreamAsync("COMPLEX_TEST", builder =>
        {
            builder
                .WithSubjects("complex.*")
                .WithDescription("Complex configuration test")
                .WithStorage(StreamConfigStorage.File)
                .WithRetention(StreamConfigRetention.Limits)
                .WithMaxMessages(50000)
                .WithMaxBytes(100 * 1024 * 1024) // 100MB
                .WithMaxAge(TimeSpan.FromDays(7))
                .WithMaxMessageSize(1024 * 1024) // 1MB
                .WithMaxConsumers(5)
                .WithDiscard(StreamConfigDiscard.Old)
                .WithDuplicateWindow(TimeSpan.FromMinutes(2));
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("COMPLEX_TEST", streamInfo.Config.Name);
        Assert.Equal("Complex configuration test", streamInfo.Config.Description);
        Assert.Equal(50000, streamInfo.Config.MaxMsgs);
        Assert.Equal(100 * 1024 * 1024, streamInfo.Config.MaxBytes);
        Assert.Equal(TimeSpan.FromDays(7), streamInfo.Config.MaxAge);
        Assert.Equal(1024 * 1024, streamInfo.Config.MaxMsgSize);
        Assert.Equal(5, streamInfo.Config.MaxConsumers);
        Assert.Equal(StreamConfigDiscard.Old, streamInfo.Config.Discard);
        Assert.Equal(TimeSpan.FromMinutes(2), streamInfo.Config.DuplicateWindow);
    }
}
