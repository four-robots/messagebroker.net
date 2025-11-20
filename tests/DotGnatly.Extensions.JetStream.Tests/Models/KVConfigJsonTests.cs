using DotGnatly.Extensions.JetStream.Models;
using NATS.Client.KeyValueStore;
using Xunit;

namespace DotGnatly.Extensions.JetStream.Tests.Models;

public class KVConfigJsonTests
{
    [Fact]
    public void FromJson_ValidJson_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""bucket"": ""USER_SESSIONS"",
            ""description"": ""User session storage"",
            ""max_history_per_key"": 5,
            ""ttl"": 7200000000000,
            ""max_bucket_size"": 1073741824,
            ""replicas"": 3,
            ""storage"": ""memory""
        }";

        // Act
        var config = KVConfigJson.FromJson(json);

        // Assert
        Assert.Equal("USER_SESSIONS", config.Bucket);
        Assert.Equal("User session storage", config.Description);
        Assert.Equal(5, config.MaxHistoryPerKey);
        Assert.Equal(7200000000000, config.TTL); // 2 hours in nanoseconds
        Assert.Equal(1073741824, config.MaxBucketSize);
        Assert.Equal(3, config.Replicas);
        Assert.Equal(KVStorageTypeJson.Memory, config.Storage);
    }

    [Fact]
    public void FromJson_MinimalJson_DeserializesWithDefaults()
    {
        // Arrange
        var json = @"{
            ""bucket"": ""TEST""
        }";

        // Act
        var config = KVConfigJson.FromJson(json);

        // Assert
        Assert.Equal("TEST", config.Bucket);
        Assert.Null(config.Description);
        Assert.Equal(1, config.MaxHistoryPerKey);
        Assert.Equal(0, config.TTL);
        Assert.Equal(-1, config.MaxBucketSize);
        Assert.Equal(1, config.Replicas);
        Assert.Equal(KVStorageTypeJson.File, config.Storage);
    }

    [Fact]
    public void ToBuilder_CreatesCorrectBuilder()
    {
        // Arrange
        var configJson = new KVConfigJson
        {
            Bucket = "USER_SESSIONS",
            Description = "User session storage",
            MaxHistoryPerKey = 5,
            TTL = 7200000000000, // 2 hours in nanoseconds
            MaxBucketSize = 1073741824,
            Replicas = 3,
            Storage = KVStorageTypeJson.Memory
        };

        // Act
        var builder = configJson.ToBuilder();
        var config = builder.Build();

        // Assert
        Assert.Equal("USER_SESSIONS", config.Bucket);
        Assert.Equal("User session storage", config.Description);
        Assert.Equal(5, config.History);
        Assert.Equal(TimeSpan.FromHours(2), config.LimitMarkerTTL);
        Assert.Equal(1073741824, config.MaxBytes);
        Assert.Equal(3, config.NumberOfReplicas);
        Assert.Equal(NatsKVStorageType.Memory, config.Storage);
    }

    [Fact]
    public void ToBuilder_WithDefaultValues_CreatesCorrectBuilder()
    {
        // Arrange
        var configJson = new KVConfigJson
        {
            Bucket = "TEST",
            MaxHistoryPerKey = 1,
            TTL = 0,
            MaxBucketSize = -1,
            Replicas = 1,
            Storage = KVStorageTypeJson.File
        };

        // Act
        var builder = configJson.ToBuilder();
        var config = builder.Build();

        // Assert
        Assert.Equal("TEST", config.Bucket);
        Assert.Null(config.Description);
        Assert.Equal(1, config.History);
        Assert.Equal(TimeSpan.Zero, config.LimitMarkerTTL);
        Assert.Equal(-1, config.MaxBytes);
        Assert.Equal(1, config.NumberOfReplicas);
        Assert.Equal(NatsKVStorageType.File, config.Storage);
    }

    [Theory]
    [InlineData(KVStorageTypeJson.File, NatsKVStorageType.File)]
    [InlineData(KVStorageTypeJson.Memory, NatsKVStorageType.Memory)]
    public void ToBuilder_ConvertsStorage(KVStorageTypeJson jsonStorage, NatsKVStorageType expectedStorage)
    {
        // Arrange
        var configJson = new KVConfigJson
        {
            Bucket = "TEST",
            Storage = jsonStorage
        };

        // Act
        var config = configJson.ToBuilder().Build();

        // Assert
        Assert.Equal(expectedStorage, config.Storage);
    }

    [Fact]
    public void ToBuilder_WithNullDescription_DoesNotSetDescription()
    {
        // Arrange
        var configJson = new KVConfigJson
        {
            Bucket = "TEST",
            Description = null
        };

        // Act
        var builder = configJson.ToBuilder();
        var config = builder.Build();

        // Assert
        Assert.Null(config.Description);
    }

    [Fact]
    public void ToBuilder_WithZeroTTL_DoesNotSetTTL()
    {
        // Arrange
        var configJson = new KVConfigJson
        {
            Bucket = "TEST",
            TTL = 0
        };

        // Act
        var builder = configJson.ToBuilder();
        var config = builder.Build();

        // Assert
        Assert.Equal(TimeSpan.Zero, config.LimitMarkerTTL);
    }

    [Fact]
    public async Task FromFileAsync_ValidFile_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""bucket"": ""TEST"",
            ""description"": ""Test bucket"",
            ""max_history_per_key"": 3,
            ""storage"": ""file""
        }";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, json, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var config = await KVConfigJson.FromFileAsync(tempFile);

            // Assert
            Assert.Equal("TEST", config.Bucket);
            Assert.Equal("Test bucket", config.Description);
            Assert.Equal(3, config.MaxHistoryPerKey);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task FromFileAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => KVConfigJson.FromFileAsync("/nonexistent/file.json"));
    }

    [Fact]
    public async Task FromDirectoryAsync_ValidDirectory_DeserializesAllFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var json1 = @"{""bucket"": ""BUCKET1"", ""description"": ""First bucket""}";
        var json2 = @"{""bucket"": ""BUCKET2"", ""description"": ""Second bucket""}";

        await File.WriteAllTextAsync(Path.Combine(tempDir, "bucket1.json"), json1, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "bucket2.json"), json2, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var configs = await KVConfigJson.FromDirectoryAsync(tempDir);

            // Assert
            Assert.Equal(2, configs.Count);
            Assert.Contains(configs, c => c.Bucket == "BUCKET1");
            Assert.Contains(configs, c => c.Bucket == "BUCKET2");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task FromDirectoryAsync_DirectoryNotFound_ThrowsDirectoryNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => KVConfigJson.FromDirectoryAsync("/nonexistent/directory"));
    }

    [Fact]
    public async Task FromDirectoryAsync_WithPattern_FiltersFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var json1 = @"{""bucket"": ""BUCKET1""}";
        var json2 = @"{""bucket"": ""BUCKET2""}";

        await File.WriteAllTextAsync(Path.Combine(tempDir, "kv-bucket1.json"), json1, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "kv-bucket2.json"), json2, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "other.json"), json1, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var configs = await KVConfigJson.FromDirectoryAsync(tempDir, "kv-*.json");

            // Assert
            Assert.Equal(2, configs.Count);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void FromJson_CaseInsensitive_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""Bucket"": ""TEST"",
            ""Description"": ""Test bucket"",
            ""Max_History_Per_Key"": 5
        }";

        // Act
        var config = KVConfigJson.FromJson(json);

        // Assert
        Assert.Equal("TEST", config.Bucket);
        Assert.Equal("Test bucket", config.Description);
        Assert.Equal(5, config.MaxHistoryPerKey);
    }

    [Fact]
    public void ToBuilder_TTLConversion_ConvertsNanosecondsToTimeSpan()
    {
        // Arrange - 1 day in nanoseconds
        var configJson = new KVConfigJson
        {
            Bucket = "TEST",
            TTL = 86400000000000 // 1 day in nanoseconds
        };

        // Act
        var builder = configJson.ToBuilder();
        var config = builder.Build();

        // Assert
        Assert.Equal(TimeSpan.FromDays(1), config.LimitMarkerTTL);
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => KVConfigJson.FromJson(json));
    }
}
