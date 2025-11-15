using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.IntegrationTests;

/// <summary>
/// Integration test suite wrapper for runtime control tests.
/// </summary>
public class RuntimeControlTestSuite : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        await results.AssertAsync("Get Server ID", RuntimeControlTests.TestGetServerId);
        await results.AssertAsync("Get Server Name", RuntimeControlTests.TestGetServerName);
        await results.AssertAsync("Is Server Running - True", RuntimeControlTests.TestIsServerRunning_True);
        await results.AssertAsync("Is Server Running - False", RuntimeControlTests.TestIsServerRunning_False);
        await results.AssertAsync("Wait For Ready", RuntimeControlTests.TestWaitForReady);
        await results.AssertAsync("Is JetStream Enabled - False", RuntimeControlTests.TestIsJetStreamEnabled_False);
        await results.AssertAsync("Is JetStream Enabled - True", RuntimeControlTests.TestIsJetStreamEnabled_True);
    }
}

/// <summary>
/// Integration tests for NATS server runtime control (GetServerId, GetServerName, IsServerRunning).
/// </summary>
public static class RuntimeControlTests
{
    public static async Task<bool> TestGetServerId()
    {
        Console.WriteLine("\n=== Testing GetServerId (Server ID Retrieval) ===");

        using var controller = new NatsController();

        // Start server
        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4238,
            Description = "Server ID test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Get server ID
            var serverId = await controller.GetServerIdAsync();
            Console.WriteLine($"✓ Retrieved server ID: {serverId}");

            if (string.IsNullOrWhiteSpace(serverId))
            {
                Console.WriteLine("❌ Server ID is null or empty");
                return false;
            }

            // Server ID should be a UUID-like string
            if (serverId.Length < 10)
            {
                Console.WriteLine($"❌ Server ID seems too short: {serverId}");
                return false;
            }

            Console.WriteLine("✓ GetServerId test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetServerId test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestGetServerName()
    {
        Console.WriteLine("\n=== Testing GetServerName (Server Name Retrieval) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4239,
            Description = "Server name test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Get server name
            var serverName = await controller.GetServerNameAsync();
            Console.WriteLine($"✓ Retrieved server name: '{serverName}'");

            // Note: Server name can be empty if not configured
            // This is a valid scenario, so we just log it
            if (string.IsNullOrEmpty(serverName))
            {
                Console.WriteLine("  (Server name not configured - this is valid)");
            }

            Console.WriteLine("✓ GetServerName test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ GetServerName test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestIsServerRunning_True()
    {
        Console.WriteLine("\n=== Testing IsServerRunning (Running State) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4240,
            Description = "Server running test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Check if server is running
            var isRunning = await controller.IsServerRunningAsync();
            Console.WriteLine($"✓ Server running status: {isRunning}");

            if (!isRunning)
            {
                Console.WriteLine("❌ Expected server to be running, but it reports as not running");
                return false;
            }

            Console.WriteLine("✓ IsServerRunning (true) test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ IsServerRunning test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestIsServerRunning_False()
    {
        Console.WriteLine("\n=== Testing IsServerRunning (Not Running State) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4241,
            Description = "Server not running test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Shutdown the server
            await controller.ShutdownAsync();
            Console.WriteLine("✓ Server shut down");

            // Wait a bit for shutdown to complete
            await Task.Delay(200);

            // Check if server is running (should be false now)
            var isRunning = await controller.IsServerRunningAsync();
            Console.WriteLine($"✓ Server running status after shutdown: {isRunning}");

            if (isRunning)
            {
                Console.WriteLine("❌ Expected server to be not running after shutdown");
                return false;
            }

            Console.WriteLine("✓ IsServerRunning (false) test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ IsServerRunning (false) test failed: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> TestWaitForReady()
    {
        Console.WriteLine("\n=== Testing WaitForReady (Health Check) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4242,
            Description = "Wait for ready test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        try
        {
            // Wait for server to be ready with a 5 second timeout
            var isReady = await controller.WaitForReadyAsync(timeoutSeconds: 5);
            Console.WriteLine($"✓ Server ready status: {isReady}");

            if (!isReady)
            {
                Console.WriteLine("❌ Expected server to be ready within timeout");
                return false;
            }

            Console.WriteLine("✓ WaitForReady test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ WaitForReady test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestIsJetStreamEnabled_False()
    {
        Console.WriteLine("\n=== Testing IsJetStreamEnabled (Not Enabled) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4243,
            Jetstream = false, // Explicitly disable JetStream
            Description = "JetStream disabled test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Check if JetStream is enabled
            var isEnabled = await controller.IsJetStreamEnabledAsync();
            Console.WriteLine($"✓ JetStream enabled status: {isEnabled}");

            if (isEnabled)
            {
                Console.WriteLine("❌ Expected JetStream to be disabled");
                return false;
            }

            Console.WriteLine("✓ IsJetStreamEnabled (false) test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ IsJetStreamEnabled test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestIsJetStreamEnabled_True()
    {
        Console.WriteLine("\n=== Testing IsJetStreamEnabled (Enabled) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4244,
            Jetstream = true, // Enable JetStream
            JetstreamStoreDir = Path.Combine(Path.GetTempPath(), "nats-js-test"),
            Description = "JetStream enabled test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Check if JetStream is enabled
            var isEnabled = await controller.IsJetStreamEnabledAsync();
            Console.WriteLine($"✓ JetStream enabled status: {isEnabled}");

            if (!isEnabled)
            {
                Console.WriteLine("❌ Expected JetStream to be enabled");
                return false;
            }

            Console.WriteLine("✓ IsJetStreamEnabled (true) test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ IsJetStreamEnabled test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();

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
