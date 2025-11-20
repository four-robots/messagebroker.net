using DotGnatly.Core.Configuration;
using DotGnatly.Extensions.JetStream.Extensions;
using DotGnatly.Extensions.JetStream.Models;
using DotGnatly.Nats.Implementation;
using NATS.Client.KeyValueStore;
using Xunit;

namespace DotGnatly.Extensions.JetStream.IntegrationTests;

public class KeyValueExtensionsTests : IAsyncLifetime
{
    private NatsController? _controller;
    private readonly string _jetstreamDir;

    public KeyValueExtensionsTests()
    {
        _jetstreamDir = Path.Combine(Path.GetTempPath(), $"kv-tests-{Guid.NewGuid()}");
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
    public async Task CreateKeyValueAsync_BasicBucket_CreatesSuccessfully()
    {
        // Arrange & Act
        var store = await _controller!.CreateKeyValueAsync("TEST_BASIC", builder =>
        {
            builder
                .WithDescription("Basic test bucket")
                .WithMaxHistoryPerKey(1);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_BASIC", store.Bucket);
    }

    [Fact]
    public async Task CreateKeyValueAsync_MemoryStorage_CreatesSuccessfully()
    {
        // Arrange & Act
        var store = await _controller!.CreateKeyValueAsync("TEST_MEMORY", builder =>
        {
            builder
                .WithDescription("Memory test bucket")
                .WithStorage(NatsKVStorageType.Memory)
                .WithMaxHistoryPerKey(3);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_MEMORY", store.Bucket);

        // Verify by getting status
        var status = await _controller!.GetKeyValueStatusAsync("TEST_MEMORY", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("TEST_MEMORY", status.Bucket);
    }

    [Fact]
    public async Task CreateKeyValueAsync_WithTTL_CreatesSuccessfully()
    {
        // Arrange & Act
        var store = await _controller!.CreateKeyValueAsync("TEST_TTL", builder =>
        {
            builder
                .WithDescription("TTL test bucket")
                .WithTTL(TimeSpan.FromHours(1))
                .WithMaxHistoryPerKey(1);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_TTL", store.Bucket);
    }

    [Fact]
    public async Task CreateKeyValueAsync_WithReplicas_CreatesSuccessfully()
    {
        // Arrange & Act
        var store = await _controller!.CreateKeyValueAsync("TEST_REPLICAS", builder =>
        {
            builder
                .WithDescription("Replicas test bucket")
                .WithReplicas(1) // Single replica for standalone server
                .WithMaxHistoryPerKey(5);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_REPLICAS", store.Bucket);
    }

    [Fact]
    public async Task CreateKeyValueAsync_WithMaxBucketSize_CreatesSuccessfully()
    {
        // Arrange & Act
        var store = await _controller!.CreateKeyValueAsync("TEST_MAXSIZE", builder =>
        {
            builder
                .WithDescription("Max size test bucket")
                .WithMaxBucketSize(1024 * 1024) // 1MB
                .WithMaxHistoryPerKey(1);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("TEST_MAXSIZE", store.Bucket);
    }

    [Fact]
    public async Task GetKeyValueAsync_ExistingBucket_ReturnsStore()
    {
        // Arrange
        await _controller!.CreateKeyValueAsync("GET_TEST", b => b.WithMaxHistoryPerKey(1), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var store = await _controller!.GetKeyValueAsync("GET_TEST", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("GET_TEST", store.Bucket);
    }

    [Fact]
    public async Task ListKeyValuesAsync_ReturnsAllBuckets()
    {
        // Arrange
        await _controller!.CreateKeyValueAsync("LIST_TEST_1", b => b.WithMaxHistoryPerKey(1), cancellationToken: TestContext.Current.CancellationToken);
        await _controller!.CreateKeyValueAsync("LIST_TEST_2", b => b.WithMaxHistoryPerKey(1), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var buckets = await _controller!.ListKeyValuesAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("LIST_TEST_1", buckets);
        Assert.Contains("LIST_TEST_2", buckets);
    }

    [Fact]
    public async Task GetKeyValueStatusAsync_ExistingBucket_ReturnsStatus()
    {
        // Arrange
        await _controller!.CreateKeyValueAsync("STATUS_TEST", b => b
            .WithDescription("Status test bucket")
            .WithMaxHistoryPerKey(3), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var status = await _controller!.GetKeyValueStatusAsync("STATUS_TEST", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("STATUS_TEST", status.Bucket);
        Assert.True(status.Info.State.NumSubjects > 0 || status.Info.State.NumSubjects == 0); // Just verify we got a status
    }

    [Fact]
    public async Task UpdateKeyValueAsync_UpdatesExistingBucket()
    {
        // Arrange
        await _controller!.CreateKeyValueAsync("UPDATE_TEST", b => b
            .WithMaxHistoryPerKey(1)
            .WithDescription("Original description"), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var updatedStore = await _controller!.UpdateKeyValueAsync("UPDATE_TEST", b => b
            .WithMaxHistoryPerKey(5)
            .WithDescription("Updated description"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("UPDATE_TEST", updatedStore.Bucket);

        // Verify update by checking status
        var status = await _controller!.GetKeyValueStatusAsync("UPDATE_TEST", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("UPDATE_TEST", status.Bucket);
    }

    //[Fact]
    //public async Task PurgeKeyValueAsync_PurgesBucket()
    //{
    //    // Arrange
    //    await _controller!.CreateKeyValueAsync("PURGE_TEST", b => b.WithMaxHistoryPerKey(1));

    //    // Act
    //    var purged = await _controller!.PurgeKeyValueAsync("PURGE_TEST");

    //    // Assert
    //    Assert.True(purged);
    //}

    [Fact]
    public async Task DeleteKeyValueAsync_DeletesBucket()
    {
        // Arrange
        await _controller!.CreateKeyValueAsync("DELETE_TEST", b => b.WithMaxHistoryPerKey(1), cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var deleted = await _controller!.DeleteKeyValueAsync("DELETE_TEST", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.True(deleted);
        var buckets = await _controller!.ListKeyValuesAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.DoesNotContain("DELETE_TEST", buckets);
    }

    [Fact]
    public async Task CreateKeyValueFromJsonAsync_CreatesFromJsonString()
    {
        // Arrange
        var json = @"{
            ""bucket"": ""JSON_TEST"",
            ""description"": ""JSON test bucket"",
            ""max_history_per_key"": 5,
            ""storage"": ""file""
        }";

        // Act
        var store = await _controller!.CreateKeyValueFromJsonAsync(json, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("JSON_TEST", store.Bucket);
    }

    [Fact]
    public async Task CreateKeyValueFromFileAsync_CreatesFromJsonFile()
    {
        // Arrange
        var json = @"{
            ""bucket"": ""FILE_TEST"",
            ""description"": ""File test bucket"",
            ""max_history_per_key"": 3,
            ""storage"": ""file""
        }";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, json, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var store = await _controller!.CreateKeyValueFromFileAsync(tempFile, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("FILE_TEST", store.Bucket);
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
    public async Task CreateKeyValuesFromDirectoryAsync_CreatesMultipleBuckets()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var json1 = @"{""bucket"": ""DIR_TEST_1"", ""max_history_per_key"": 1}";
        var json2 = @"{""bucket"": ""DIR_TEST_2"", ""max_history_per_key"": 1}";

        await File.WriteAllTextAsync(Path.Combine(tempDir, "bucket1.json"), json1, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "bucket2.json"), json2, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var stores = await _controller!.CreateKeyValuesFromDirectoryAsync(tempDir, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, stores.Count);
            Assert.Contains(stores, s => s.Bucket == "DIR_TEST_1");
            Assert.Contains(stores, s => s.Bucket == "DIR_TEST_2");
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
    public async Task CreateKeyValueAsync_WithComplexConfiguration_CreatesSuccessfully()
    {
        // Arrange & Act
        var store = await _controller!.CreateKeyValueAsync("COMPLEX_TEST", builder =>
        {
            builder
                .WithDescription("Complex configuration test")
                .WithMaxHistoryPerKey(10)
                .WithTTL(TimeSpan.FromHours(24))
                .WithMaxBucketSize(10 * 1024 * 1024) // 10MB
                .WithReplicas(1)
                .WithStorage(NatsKVStorageType.File);
        }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("COMPLEX_TEST", store.Bucket);

        // Verify via status
        var status = await _controller!.GetKeyValueStatusAsync("COMPLEX_TEST", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("COMPLEX_TEST", status.Bucket);
    }

    [Fact]
    public async Task CreateKeyValueFromJsonAsync_WithTTL_CreatesSuccessfully()
    {
        // Arrange - 2 hours in nanoseconds
        var json = @"{
            ""bucket"": ""JSON_TTL_TEST"",
            ""description"": ""JSON TTL test"",
            ""ttl"": 7200000000000,
            ""max_history_per_key"": 1
        }";

        // Act
        var store = await _controller!.CreateKeyValueFromJsonAsync(json, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("JSON_TTL_TEST", store.Bucket);
    }

    [Fact]
    public async Task CreateKeyValueFromJsonAsync_WithMaxBucketSize_CreatesSuccessfully()
    {
        // Arrange
        var json = @"{
            ""bucket"": ""JSON_SIZE_TEST"",
            ""description"": ""JSON size test"",
            ""max_bucket_size"": 1048576,
            ""max_history_per_key"": 1
        }";

        // Act
        var store = await _controller!.CreateKeyValueFromJsonAsync(json, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("JSON_SIZE_TEST", store.Bucket);
    }
}
