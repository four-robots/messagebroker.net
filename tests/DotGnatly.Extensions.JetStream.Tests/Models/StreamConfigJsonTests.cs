using DotGnatly.Extensions.JetStream.Models;
using NATS.Client.JetStream.Models;
using Xunit;

namespace DotGnatly.Extensions.JetStream.Tests.Models;

public class StreamConfigJsonTests
{
    [Fact]
    public void FromJson_ValidJson_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""name"": ""ORDERS"",
            ""subjects"": [""orders.*""],
            ""retention"": ""limits"",
            ""max_msgs"": 10000,
            ""max_bytes"": 1073741824,
            ""max_age"": 2592000000000000,
            ""max_msg_size"": 1048576,
            ""max_consumers"": 10,
            ""max_msgs_per_subject"": 1000,
            ""storage"": ""file"",
            ""discard"": ""old"",
            ""num_replicas"": 3,
            ""duplicate_window"": 120000000000,
            ""description"": ""Test stream""
        }";

        // Act
        var config = StreamConfigJson.FromJson(json);

        // Assert
        Assert.Equal("ORDERS", config.Name);
        Assert.Single(config.Subjects);
        Assert.Contains("orders.*", config.Subjects);
        Assert.Equal(RetentionJson.Limits, config.Retention);
        Assert.Equal(10000, config.MaxMsgs);
        Assert.Equal(1073741824, config.MaxBytes);
        Assert.Equal(2592000000000000, config.MaxAge);
        Assert.Equal(1048576, config.MaxMsgSize);
        Assert.Equal(10, config.MaxConsumers);
        Assert.Equal(1000, config.MaxMsgsPerSubject);
        Assert.Equal(StorageJson.File, config.Storage);
        Assert.Equal(DiscardJson.Old, config.Discard);
        Assert.Equal(3, config.NumReplicas);
        Assert.Equal(120000000000, config.DuplicateWindow);
        Assert.Equal("Test stream", config.Description);
    }

    [Fact]
    public void FromJson_WithPlacement_DeserializesPlacement()
    {
        // Arrange
        var json = @"{
            ""name"": ""TEST"",
            ""subjects"": [""test.*""],
            ""placement"": {
                ""cluster"": ""cluster-1"",
                ""tags"": [""tag1"", ""tag2""]
            }
        }";

        // Act
        var config = StreamConfigJson.FromJson(json);

        // Assert
        Assert.NotNull(config.Placement);
        Assert.Equal("cluster-1", config.Placement!.Cluster);
        Assert.Equal(2, config.Placement.Tags!.Count);
        Assert.Contains("tag1", config.Placement.Tags);
        Assert.Contains("tag2", config.Placement.Tags);
    }

    [Fact]
    public void FromJson_WithSources_DeserializesSources()
    {
        // Arrange
        var json = @"{
            ""name"": ""TEST"",
            ""subjects"": [""test.*""],
            ""sources"": [
                {
                    ""name"": ""SOURCE1"",
                    ""filter_subject"": ""source.*"",
                    ""external"": {
                        ""api"": ""$JS.hub.API"",
                        ""deliver"": ""deliver.hub""
                    }
                }
            ]
        }";

        // Act
        var config = StreamConfigJson.FromJson(json);

        // Assert
        Assert.NotNull(config.Sources);
        Assert.Single(config.Sources);
        Assert.Equal("SOURCE1", config.Sources![0].Name);
        Assert.Equal("source.*", config.Sources[0].FilterSubject);
        Assert.NotNull(config.Sources[0].External);
        Assert.Equal("$JS.hub.API", config.Sources[0].External!.Api);
        Assert.NotEmpty(config.Sources);
        Assert.NotNull(config.Sources[0]?.External);
        Assert.Equal("deliver.hub", config.Sources[0].External?.Deliver);
    }

    [Fact]
    public void FromJson_WithAdvancedFlags_DeserializesFlags()
    {
        // Arrange
        var json = @"{
            ""name"": ""TEST"",
            ""subjects"": [""test.*""],
            ""sealed"": true,
            ""deny_delete"": true,
            ""deny_purge"": false,
            ""allow_rollup_hdrs"": true,
            ""allow_direct"": true
        }";

        // Act
        var config = StreamConfigJson.FromJson(json);

        // Assert
        Assert.True(config.Sealed);
        Assert.True(config.DenyDelete);
        Assert.False(config.DenyPurge);
        Assert.True(config.AllowRollupHdrs);
        Assert.True(config.AllowDirect);
    }

    [Fact]
    public void ToBuilder_CreatesCorrectBuilder()
    {
        // Arrange
        var configJson = new StreamConfigJson
        {
            Name = "ORDERS",
            Subjects = new List<string> { "orders.*", "shipments.*" },
            Description = "Test stream",
            Retention = RetentionJson.Limits,
            Storage = StorageJson.File,
            MaxMsgs = 10000,
            MaxBytes = 1073741824,
            NumReplicas = 3,
            MaxConsumers = 10,
            MaxMsgsPerSubject = 1000
        };

        // Act
        var builder = configJson.ToBuilder();
        var config = builder.Build();

        // Assert
        Assert.Equal("ORDERS", config.Name);
        Assert.NotNull(config.Subjects);
        Assert.NotEmpty(config.Subjects);
        Assert.Equal(2, config.Subjects.Count);
        Assert.Contains("orders.*", config.Subjects);
        Assert.Contains("shipments.*", config.Subjects);
        Assert.Equal("Test stream", config.Description);
        Assert.Equal(StreamConfigRetention.Limits, config.Retention);
        Assert.Equal(StreamConfigStorage.File, config.Storage);
        Assert.Equal(10000, config.MaxMsgs);
        Assert.Equal(1073741824, config.MaxBytes);
        Assert.Equal(3, config.NumReplicas);
        Assert.Equal(10, config.MaxConsumers);
        Assert.Equal(1000, config.MaxMsgsPerSubject);
    }

    [Fact]
    public void ToBuilder_WithAdvancedFeatures_CreatesCorrectBuilder()
    {
        // Arrange
        var configJson = new StreamConfigJson
        {
            Name = "KV_TEST",
            Subjects = new List<string> { "$KV.test.>" },
            MaxMsgsPerSubject = 1,
            DenyDelete = true,
            AllowRollupHdrs = true,
            AllowDirect = true,
            Placement = new PlacementJson
            {
                Cluster = "cluster-1",
                Tags = new List<string> { "tag1" }
            },
            Sources = new List<StreamSourceJson>
            {
                new StreamSourceJson
                {
                    Name = "SOURCE",
                    FilterSubject = "source.*",
                    External = new ExternalJson
                    {
                        Api = "$JS.hub.API",
                        Deliver = "deliver.hub"
                    }
                }
            }
        };

        // Act
        var builder = configJson.ToBuilder();
        var config = builder.Build();

        // Assert
        Assert.Equal("KV_TEST", config.Name);
        Assert.Equal(1, config.MaxMsgsPerSubject);
        Assert.True(config.DenyDelete);
        Assert.True(config.AllowRollupHdrs);
        Assert.True(config.AllowDirect);
        Assert.NotNull(config.Placement);
        Assert.Equal("cluster-1", config.Placement!.Cluster);
        Assert.NotNull(config.Sources);
        Assert.Single(config.Sources);
        Assert.Equal("SOURCE", config.Sources.First().Name);
    }

    [Theory]
    [InlineData(RetentionJson.Limits, StreamConfigRetention.Limits)]
    [InlineData(RetentionJson.Interest, StreamConfigRetention.Interest)]
    [InlineData(RetentionJson.Workqueue, StreamConfigRetention.Workqueue)]
    public void ToBuilder_ConvertsRetention(RetentionJson jsonRetention, StreamConfigRetention expectedRetention)
    {
        // Arrange
        var configJson = new StreamConfigJson
        {
            Name = "TEST",
            Subjects = new List<string> { "test.*" },
            Retention = jsonRetention
        };

        // Act
        var config = configJson.ToBuilder().Build();

        // Assert
        Assert.Equal(expectedRetention, config.Retention);
    }

    [Theory]
    [InlineData(StorageJson.File, StreamConfigStorage.File)]
    [InlineData(StorageJson.Memory, StreamConfigStorage.Memory)]
    public void ToBuilder_ConvertsStorage(StorageJson jsonStorage, StreamConfigStorage expectedStorage)
    {
        // Arrange
        var configJson = new StreamConfigJson
        {
            Name = "TEST",
            Subjects = new List<string> { "test.*" },
            Storage = jsonStorage
        };

        // Act
        var config = configJson.ToBuilder().Build();

        // Assert
        Assert.Equal(expectedStorage, config.Storage);
    }

    [Theory]
    [InlineData(DiscardJson.Old, StreamConfigDiscard.Old)]
    [InlineData(DiscardJson.New, StreamConfigDiscard.New)]
    public void ToBuilder_ConvertsDiscard(DiscardJson jsonDiscard, StreamConfigDiscard expectedDiscard)
    {
        // Arrange
        var configJson = new StreamConfigJson
        {
            Name = "TEST",
            Subjects = new List<string> { "test.*" },
            Discard = jsonDiscard
        };

        // Act
        var config = configJson.ToBuilder().Build();

        // Assert
        Assert.Equal(expectedDiscard, config.Discard);
    }

    [Fact]
    public async Task FromFileAsync_ValidFile_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""name"": ""TEST"",
            ""subjects"": [""test.*""],
            ""retention"": ""limits"",
            ""storage"": ""file""
        }";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, json, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var config = await StreamConfigJson.FromFileAsync(tempFile);

            // Assert
            Assert.Equal("TEST", config.Name);
            Assert.Single(config.Subjects);
            Assert.Equal("test.*", config.Subjects[0]);
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
            () => StreamConfigJson.FromFileAsync("/nonexistent/file.json"));
    }

    [Fact]
    public async Task FromDirectoryAsync_ValidDirectory_DeserializesAllFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var json1 = @"{""name"": ""STREAM1"", ""subjects"": [""stream1.*""]}";
        var json2 = @"{""name"": ""STREAM2"", ""subjects"": [""stream2.*""]}";

        await File.WriteAllTextAsync(Path.Combine(tempDir, "stream1.json"), json1, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "stream2.json"), json2, TestContext.Current.CancellationToken);

        try
        {
            // Act
            var configs = await StreamConfigJson.FromDirectoryAsync(tempDir);

            // Assert
            Assert.Equal(2, configs.Count);
            Assert.Contains(configs, c => c.Name == "STREAM1");
            Assert.Contains(configs, c => c.Name == "STREAM2");
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
            () => StreamConfigJson.FromDirectoryAsync("/nonexistent/directory"));
    }
}
