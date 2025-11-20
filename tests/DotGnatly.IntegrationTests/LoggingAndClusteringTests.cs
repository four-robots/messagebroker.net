using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using System.Text.Json;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests for logging control and JetStream clustering features.
/// </summary>
public class LoggingAndClusteringTests
{
    [Fact]
    public async Task LogFileConfigurationCanBeSet()
    {
        using var server = new NatsController();

        var logFilePath = Path.Combine(Path.GetTempPath(), $"nats-test-{Guid.NewGuid()}.log");

        var config = new BrokerConfiguration
        {
            Port = 14222,
            LogFile = logFilePath,
            LogTimeUtc = true,
            LogFileSize = 1024 * 1024 // 1MB
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // Clean up log file
        try
        {
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Assert.Equal(logFilePath, info.CurrentConfig.LogFile);
        Assert.True(info.CurrentConfig.LogTimeUtc);
        Assert.Equal(1024 * 1024, info.CurrentConfig.LogFileSize);
    }

    [Fact]
    public async Task LogFileIsCreatedWhenLogFileIsConfigured()
    {
        using var server = new NatsController();

        var logFilePath = Path.Combine(Path.GetTempPath(), $"nats-test-{Guid.NewGuid()}.log");

        var config = new BrokerConfiguration
        {
            Port = 14223,
            LogFile = logFilePath,
            Debug = true // Generate some log output
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success);

        // Wait a bit for log file to be created
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        var logFileExists = File.Exists(logFilePath);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // Clean up log file
        try
        {
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Assert.True(logFileExists);
    }

    [Fact]
    public async Task LogTimeUtcCanBeToggledViaHotReload()
    {
        using var server = new NatsController();

        await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14224,
            LogTimeUtc = true
        }, TestContext.Current.CancellationToken);

        var result = await server.ApplyChangesAsync(c => c.LogTimeUtc = false, TestContext.Current.CancellationToken);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.False(info.CurrentConfig.LogTimeUtc);
    }

    [Fact]
    public async Task ReOpenLogFileSucceedsWhenLogFileIsConfigured()
    {
        using var server = new NatsController();

        var logFilePath = Path.Combine(Path.GetTempPath(), $"nats-test-{Guid.NewGuid()}.log");

        var config = new BrokerConfiguration
        {
            Port = 4225,
            LogFile = logFilePath,
            Debug = true
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success);

        // Wait for log file to be created
        await Task.Delay(1000, TestContext.Current.CancellationToken);

        bool success = false;
        try
        {
            // Call ReOpenLogFile - this should succeed even if no rotation happened
            // In production, external tools (logrotate, etc.) would handle the actual rotation
            await server.ReOpenLogFileAsync(TestContext.Current.CancellationToken);

            success = true;
        }
        catch (Exception)
        {
            success = false;
        }
        finally
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);

            // Clean up
            try
            {
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        Assert.True(success);
    }

    [Fact]
    public async Task ReOpenLogFileSucceedsWhenNoLogFileIsConfigured()
    {
        using var server = new NatsController();

        await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 4226
            // No LogFile configured
        }, TestContext.Current.CancellationToken);

        // This should not throw even though no log file is configured
        try
        {
            await server.ReOpenLogFileAsync(TestContext.Current.CancellationToken);
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
            Assert.True(true);
        }
        catch
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
            Assert.Fail("ReOpenLogFileAsync should not throw when no log file is configured");
        }
    }

    [Fact]
    public async Task GetOptsReturnsValidJson()
    {
        using var server = new NatsController();

        await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 4227,
            Debug = true,
            MaxPayload = 2048
        }, TestContext.Current.CancellationToken);

        var opts = await server.GetOptsAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // Verify it's valid JSON
        var jsonDoc = JsonDocument.Parse(opts);
        Assert.NotNull(jsonDoc);
    }

    [Fact]
    public async Task GetOptsContainsExpectedConfigurationKeys()
    {
        using var server = new NatsController();

        await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 4228,
            Debug = true,
            MaxPayload = 2048,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-opts-test")
        }, TestContext.Current.CancellationToken);

        var opts = await server.GetOptsAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        var jsonDoc = JsonDocument.Parse(opts);
        var root = jsonDoc.RootElement;

        // Check for common configuration keys
        var hasPort = root.TryGetProperty("port", out _);
        var hasMaxPayload = root.TryGetProperty("max_payload", out _) ||
                           root.TryGetProperty("maxPayload", out _);

        // Clean up JetStream directory
        try
        {
            var jsDir = Path.Combine(Path.GetTempPath(), "nats-js-opts-test");
            if (Directory.Exists(jsDir))
            {
                Directory.Delete(jsDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Assert.True(hasPort || hasMaxPayload); // At least one key should be present
    }

    [Fact]
    public async Task GetOptsReflectsCurrentConfigurationAfterHotReload()
    {
        using var server = new NatsController();

        await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 4229,
            Debug = false
        }, TestContext.Current.CancellationToken);

        // Get opts before change
        var optsBefore = await server.GetOptsAsync(TestContext.Current.CancellationToken);

        // Apply change
        await server.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);

        // Get opts after change
        var optsAfter = await server.GetOptsAsync(TestContext.Current.CancellationToken);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // The JSON should be different
        Assert.NotEqual(optsBefore, optsAfter);
    }

    [Fact]
    public async Task JetStreamDomainCanBeConfigured()
    {
        using var server = new NatsController();

        var config = new BrokerConfiguration
        {
            Port = 4230,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-domain-test"),
            JetstreamDomain = "test-domain"
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // Clean up JetStream directory
        try
        {
            if (Directory.Exists(config.JetstreamStoreDir))
            {
                Directory.Delete(config.JetstreamStoreDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Assert.Equal("test-domain", info.CurrentConfig.JetstreamDomain);
    }

    [Fact]
    public async Task JetStreamUniqueTagCanBeConfigured()
    {
        using var server = new NatsController();

        var uniqueTag = $"server-{Guid.NewGuid()}";
        var config = new BrokerConfiguration
        {
            Port = 4231,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-tag-test"),
            JetstreamUniqueTag = uniqueTag
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // Clean up JetStream directory
        try
        {
            if (Directory.Exists(config.JetstreamStoreDir))
            {
                Directory.Delete(config.JetstreamStoreDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Assert.Equal(uniqueTag, info.CurrentConfig.JetstreamUniqueTag);
    }

    [Fact]
    public async Task JetStreamDomainAndTagCanBeConfiguredTogether()
    {
        using var server = new NatsController();

        var uniqueTag = $"server-{Guid.NewGuid()}";
        var config = new BrokerConfiguration
        {
            Port = 4232,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-cluster-test"),
            JetstreamDomain = "cluster-domain",
            JetstreamUniqueTag = uniqueTag
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // Clean up JetStream directory
        try
        {
            if (Directory.Exists(config.JetstreamStoreDir))
            {
                Directory.Delete(config.JetstreamStoreDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Assert.Equal("cluster-domain", info.CurrentConfig.JetstreamDomain);
        Assert.Equal(uniqueTag, info.CurrentConfig.JetstreamUniqueTag);
    }

    [Fact]
    public async Task JetStreamClusteringPropertiesAreSetAtStartup()
    {
        using var server = new NatsController();

        var newTag = $"server-{Guid.NewGuid()}";
        await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 4233,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-reload-test"),
            JetstreamDomain = "initial-domain",
            JetstreamUniqueTag = newTag
        }, TestContext.Current.CancellationToken);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // Clean up JetStream directory
        try
        {
            var jsDir = Path.Combine(Path.GetTempPath(), "nats-js-reload-test");
            if (Directory.Exists(jsDir))
            {
                Directory.Delete(jsDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        // Verify properties were set at startup
        Assert.Equal("initial-domain", info.CurrentConfig.JetstreamDomain);
        Assert.Equal(newTag, info.CurrentConfig.JetstreamUniqueTag);
    }

    [Fact]
    public async Task LoggingAndClusteringFeaturesCanBeConfiguredTogether()
    {
        using var server = new NatsController();

        var logFilePath = Path.Combine(Path.GetTempPath(), $"nats-combined-{Guid.NewGuid()}.log");
        var uniqueTag = $"server-{Guid.NewGuid()}";

        var config = new BrokerConfiguration
        {
            Port = 4234,
            LogFile = logFilePath,
            LogTimeUtc = true,
            LogFileSize = 512 * 1024,
            Jetstream = true,
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-combined-test"),
            JetstreamDomain = "production",
            JetstreamUniqueTag = uniqueTag
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        // Clean up
        try
        {
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
            var jsDir = Path.Combine(Path.GetTempPath(), "nats-js-combined-test");
            if (Directory.Exists(jsDir))
            {
                Directory.Delete(jsDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Assert.Equal(logFilePath, info.CurrentConfig.LogFile);
        Assert.True(info.CurrentConfig.LogTimeUtc);
        Assert.Equal(512 * 1024, info.CurrentConfig.LogFileSize);
        Assert.Equal("production", info.CurrentConfig.JetstreamDomain);
        Assert.Equal(uniqueTag, info.CurrentConfig.JetstreamUniqueTag);
    }

    [Fact]
    public async Task LogFileSizeValidationAcceptsValidValues()
    {
        using var server = new NatsController();

        var config = new BrokerConfiguration
        {
            Port = 4235,
            LogFileSize = 10 * 1024 * 1024 // 10MB - valid
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task NegativeLogFileSizeIsRejectedByValidation()
    {
        using var server = new NatsController();

        var config = new BrokerConfiguration
        {
            Port = 4236,
            LogFileSize = -1 // Invalid
        };

        var result = await server.ConfigureAsync(config, TestContext.Current.CancellationToken);

        // Server might not start, so only shutdown if it did
        if (result.Success)
        {
            await server.ShutdownAsync(TestContext.Current.CancellationToken);
        }

        Assert.False(result.Success);
    }
}
