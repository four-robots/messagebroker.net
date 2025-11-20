using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Integration tests for NATS server runtime control (GetServerId, GetServerName, IsServerRunning).
/// </summary>
public class RuntimeControlTests
{
    [Fact]
    public async Task TestGetServerId()
    {
        using var controller = new NatsController();

        // Start server
        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4238,
            Description = "Server ID test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get server ID
            var serverId = await controller.GetServerIdAsync(TestContext.Current.CancellationToken);

            Assert.False(string.IsNullOrWhiteSpace(serverId), "Server ID should not be null or empty");

            // Server ID should be a UUID-like string
            Assert.True(serverId.Length >= 10, $"Server ID seems too short: {serverId}");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestGetServerName()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4239,
            Description = "Server name test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Get server name
            var serverName = await controller.GetServerNameAsync(TestContext.Current.CancellationToken);

            // Note: Server name can be empty if not configured
            // This is a valid scenario, so we just verify the call doesn't throw
            Assert.NotNull(serverName);
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestIsServerRunning_True()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4240,
            Description = "Server running test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Check if server is running
            var isRunning = await controller.IsServerRunningAsync(TestContext.Current.CancellationToken);

            Assert.True(isRunning, "Expected server to be running, but it reports as not running");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestIsServerRunning_False()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4241,
            Description = "Server not running test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        // Shutdown the server
        await controller.ShutdownAsync(TestContext.Current.CancellationToken);

        // Wait a bit for shutdown to complete
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Check if server is running (should be false now)
        var isRunning = await controller.IsServerRunningAsync(TestContext.Current.CancellationToken);

        Assert.False(isRunning, "Expected server to be not running after shutdown");
    }

    [Fact]
    public async Task TestWaitForReady()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4242,
            Description = "Wait for ready test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        try
        {
            // Wait for server to be ready with a 5 second timeout
            var isReady = await controller.WaitForReadyAsync(timeoutSeconds: 5, cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(isReady, "Expected server to be ready within timeout");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestIsJetStreamEnabled_False()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4243,
            Jetstream = false, // Explicitly disable JetStream
            Description = "JetStream disabled test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Check if JetStream is enabled
            var isEnabled = await controller.IsJetStreamEnabledAsync(TestContext.Current.CancellationToken);

            Assert.False(isEnabled, "Expected JetStream to be disabled");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestIsJetStreamEnabled_True()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4244,
            Jetstream = true, // Enable JetStream
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-test"),
            Description = "JetStream enabled test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Check if JetStream is enabled
            var isEnabled = await controller.IsJetStreamEnabledAsync(TestContext.Current.CancellationToken);

            Assert.True(isEnabled, "Expected JetStream to be enabled");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);

            // Clean up JetStream store directory
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
        }
    }
}
