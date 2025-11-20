using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using System.Text.Json;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests for logging control and JetStream clustering features.
/// </summary>
public class LoggingAndClusteringTests : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        // Logging Configuration Tests
        await results.AssertAsync(
            "Log file configuration can be set",
            async () =>
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

                var result = await server.ConfigureAsync(config);
                if (!result.Success)
                {
                    return false;
                }

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

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

                return info.CurrentConfig.LogFile == logFilePath &&
                       info.CurrentConfig.LogTimeUtc == true &&
                       info.CurrentConfig.LogFileSize == 1024 * 1024;
            });

        await results.AssertAsync(
            "Log file is created when LogFile is configured",
            async () =>
            {
                using var server = new NatsController();

                var logFilePath = Path.Combine(Path.GetTempPath(), $"nats-test-{Guid.NewGuid()}.log");

                var config = new BrokerConfiguration
                {
                    Port = 14223,
                    LogFile = logFilePath,
                    Debug = true // Generate some log output
                };

                var result = await server.ConfigureAsync(config);
                if (!result.Success)
                {
                    return false;
                }

                // Wait a bit for log file to be created
                await Task.Delay(1000);

                var logFileExists = File.Exists(logFilePath);

                await server.ShutdownAsync();

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

                return logFileExists;
            });

        await results.AssertAsync(
            "LogTimeUtc can be toggled via hot reload",
            async () =>
            {
                using var server = new NatsController();

                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 14224,
                    LogTimeUtc = true
                });

                var result = await server.ApplyChangesAsync(c => c.LogTimeUtc = false);

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

                return result.Success && info.CurrentConfig.LogTimeUtc == false;
            });

        await results.AssertAsync(
            "ReOpenLogFile succeeds when log file is configured",
            async () =>
            {
                using var server = new NatsController();

                var logFilePath = Path.Combine(Path.GetTempPath(), $"nats-test-{Guid.NewGuid()}.log");

                var config = new BrokerConfiguration
                {
                    Port = 4225,
                    LogFile = logFilePath,
                    Debug = true
                };

                var result = await server.ConfigureAsync(config);
                if (!result.Success)
                {
                    return false;
                }

                // Wait for log file to be created
                await Task.Delay(1000);

                bool success = false;
                try
                {
                    // Call ReOpenLogFile - this should succeed even if no rotation happened
                    // In production, external tools (logrotate, etc.) would handle the actual rotation
                    await server.ReOpenLogFileAsync();

                    success = true;
                }
                catch (Exception)
                {
                    success = false;
                }
                finally
                {
                    await server.ShutdownAsync();

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

                return success;
            });

        await results.AssertAsync(
            "ReOpenLogFile succeeds when no log file is configured",
            async () =>
            {
                using var server = new NatsController();

                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4226
                    // No LogFile configured
                });

                // This should not throw even though no log file is configured
                try
                {
                    await server.ReOpenLogFileAsync();
                    await server.ShutdownAsync();
                    return true;
                }
                catch
                {
                    await server.ShutdownAsync();
                    return false;
                }
            });

        // Configuration Introspection Tests
        await results.AssertAsync(
            "GetOpts returns valid JSON",
            async () =>
            {
                using var server = new NatsController();

                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4227,
                    Debug = true,
                    MaxPayload = 2048
                });

                var opts = await server.GetOptsAsync();
                await server.ShutdownAsync();

                // Verify it's valid JSON
                try
                {
                    var jsonDoc = JsonDocument.Parse(opts);
                    return jsonDoc != null;
                }
                catch
                {
                    return false;
                }
            });

        await results.AssertAsync(
            "GetOpts contains expected configuration keys",
            async () =>
            {
                using var server = new NatsController();

                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4228,
                    Debug = true,
                    MaxPayload = 2048,
                    Jetstream = true,
                    JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-opts-test")
                });

                var opts = await server.GetOptsAsync();
                await server.ShutdownAsync();

                try
                {
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

                    return hasPort || hasMaxPayload; // At least one key should be present
                }
                catch
                {
                    return false;
                }
            });

        await results.AssertAsync(
            "GetOpts reflects current configuration after hot reload",
            async () =>
            {
                using var server = new NatsController();

                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4229,
                    Debug = false
                });

                // Get opts before change
                var optsBefore = await server.GetOptsAsync();

                // Apply change
                await server.ApplyChangesAsync(c => c.Debug = true);

                // Get opts after change
                var optsAfter = await server.GetOptsAsync();

                await server.ShutdownAsync();

                // The JSON should be different
                return optsBefore != optsAfter;
            });

        // JetStream Clustering Tests
        await results.AssertAsync(
            "JetStream domain can be configured",
            async () =>
            {
                using var server = new NatsController();

                var config = new BrokerConfiguration
                {
                    Port = 4230,
                    Jetstream = true,
                    JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-domain-test"),
                    JetstreamDomain = "test-domain"
                };

                var result = await server.ConfigureAsync(config);
                if (!result.Success)
                {
                    return false;
                }

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

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

                return info.CurrentConfig.JetstreamDomain == "test-domain";
            });

        await results.AssertAsync(
            "JetStream unique tag can be configured",
            async () =>
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

                var result = await server.ConfigureAsync(config);
                if (!result.Success)
                {
                    return false;
                }

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

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

                return info.CurrentConfig.JetstreamUniqueTag == uniqueTag;
            });

        await results.AssertAsync(
            "JetStream domain and tag can be configured together",
            async () =>
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

                var result = await server.ConfigureAsync(config);
                if (!result.Success)
                {
                    return false;
                }

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

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

                return info.CurrentConfig.JetstreamDomain == "cluster-domain" &&
                       info.CurrentConfig.JetstreamUniqueTag == uniqueTag;
            });

        await results.AssertAsync(
            "JetStream clustering properties are set at startup",
            async () =>
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
                });

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

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
                return info.CurrentConfig.JetstreamDomain == "initial-domain" &&
                       info.CurrentConfig.JetstreamUniqueTag == newTag;
            });

        // Combined Features Test
        await results.AssertAsync(
            "Logging and clustering features can be configured together",
            async () =>
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

                var result = await server.ConfigureAsync(config);
                if (!result.Success)
                {
                    return false;
                }

                var info = await server.GetInfoAsync();
                await server.ShutdownAsync();

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

                return info.CurrentConfig.LogFile == logFilePath &&
                       info.CurrentConfig.LogTimeUtc == true &&
                       info.CurrentConfig.LogFileSize == 512 * 1024 &&
                       info.CurrentConfig.JetstreamDomain == "production" &&
                       info.CurrentConfig.JetstreamUniqueTag == uniqueTag;
            });

        // Validation Tests
        await results.AssertAsync(
            "Log file size validation accepts valid values",
            async () =>
            {
                using var server = new NatsController();

                var config = new BrokerConfiguration
                {
                    Port = 4235,
                    LogFileSize = 10 * 1024 * 1024 // 10MB - valid
                };

                var result = await server.ConfigureAsync(config);
                await server.ShutdownAsync();

                return result.Success;
            });

        await results.AssertAsync(
            "Negative log file size is rejected by validation",
            async () =>
            {
                using var server = new NatsController();

                var config = new BrokerConfiguration
                {
                    Port = 4236,
                    LogFileSize = -1 // Invalid
                };

                var result = await server.ConfigureAsync(config);

                // Server might not start, so only shutdown if it did
                if (result.Success)
                {
                    await server.ShutdownAsync();
                }

                return !result.Success;
            });
    }
}
