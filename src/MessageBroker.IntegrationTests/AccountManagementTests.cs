using System.Text.Json;
using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.IntegrationTests;

/// <summary>
/// Integration test suite wrapper for account management tests.
/// </summary>
public class AccountManagementTestSuite : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        await results.AssertAsync("Register Account", AccountManagementTests.TestRegisterAccount);
        await results.AssertAsync("Lookup Account", AccountManagementTests.TestLookupAccount);
        await results.AssertAsync("Lookup Non-Existent Account", AccountManagementTests.TestLookupNonExistentAccount);
        await results.AssertAsync("Account Statistics", AccountManagementTests.TestAccountStatz);
        await results.AssertAsync("Account Statistics With Filter", AccountManagementTests.TestAccountStatzWithFilter);
    }
}

/// <summary>
/// Integration tests for NATS server account management (RegisterAccount, LookupAccount, AccountStatz).
/// </summary>
public static class AccountManagementTests
{
    public static async Task<bool> TestRegisterAccount()
    {
        Console.WriteLine("\n=== Testing RegisterAccount (Account Registration) ===");

        using var controller = new NatsController();

        // Start server
        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4233,
            Description = "Account registration test"
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
            // Register a new account
            var accountJson = await controller.RegisterAccountAsync("TEST_ACCOUNT_001");
            Console.WriteLine($"✓ Registered account");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(accountJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("account", out var accountName))
            {
                Console.WriteLine($"  Account name: {accountName.GetString()}");
                if (accountName.GetString() != "TEST_ACCOUNT_001")
                {
                    Console.WriteLine("❌ Account name mismatch");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("❌ Missing 'account' field in response");
                return false;
            }

            if (root.TryGetProperty("connections", out var connections))
            {
                Console.WriteLine($"  Connections: {connections.GetInt32()}");
            }

            if (root.TryGetProperty("jetstream", out var jetstream))
            {
                Console.WriteLine($"  JetStream enabled: {jetstream.GetBoolean()}");
            }

            // Test duplicate registration (should fail)
            try
            {
                await controller.RegisterAccountAsync("TEST_ACCOUNT_001");
                Console.WriteLine("❌ Expected duplicate account registration to fail");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✓ Duplicate registration correctly rejected: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}...");
            }

            Console.WriteLine("✓ RegisterAccount test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ RegisterAccount test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestLookupAccount()
    {
        Console.WriteLine("\n=== Testing LookupAccount (Account Lookup) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4234,
            Description = "Account lookup test"
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
            // Register an account first
            var registerJson = await controller.RegisterAccountAsync("TEST_LOOKUP_ACCOUNT");
            Console.WriteLine($"✓ Registered test account");

            // Now look it up
            var lookupJson = await controller.LookupAccountAsync("TEST_LOOKUP_ACCOUNT");
            Console.WriteLine($"✓ Looked up account");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(lookupJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("account", out var accountName))
            {
                Console.WriteLine($"  Account name: {accountName.GetString()}");
                if (accountName.GetString() != "TEST_LOOKUP_ACCOUNT")
                {
                    Console.WriteLine("❌ Account name mismatch in lookup");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("❌ Missing 'account' field in lookup response");
                return false;
            }

            if (root.TryGetProperty("connections", out var connections))
            {
                Console.WriteLine($"  Connections: {connections.GetInt32()}");
            }

            if (root.TryGetProperty("subscriptions", out var subscriptions))
            {
                Console.WriteLine($"  Subscriptions: {subscriptions.GetInt32()}");
            }

            if (root.TryGetProperty("total_subs", out var totalSubs))
            {
                Console.WriteLine($"  Total subscriptions: {totalSubs.GetInt32()}");
            }

            Console.WriteLine("✓ LookupAccount test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ LookupAccount test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestLookupNonExistentAccount()
    {
        Console.WriteLine("\n=== Testing LookupAccount for Non-Existent Account ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4235,
            Description = "Account lookup error test"
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
            // Try to lookup non-existent account (should throw)
            try
            {
                await controller.LookupAccountAsync("NONEXISTENT_ACCOUNT");
                Console.WriteLine("❌ Expected lookup of non-existent account to fail");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✓ Lookup of non-existent account correctly failed: {ex.Message.Substring(0, Math.Min(60, ex.Message.Length))}...");
            }

            Console.WriteLine("✓ Lookup non-existent account test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lookup non-existent account test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestAccountStatz()
    {
        Console.WriteLine("\n=== Testing GetAccountStatz (Account Statistics) ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4236,
            Description = "Account statistics test"
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
            // Register some test accounts
            await controller.RegisterAccountAsync("STATS_ACCOUNT_001");
            await controller.RegisterAccountAsync("STATS_ACCOUNT_002");
            Console.WriteLine($"✓ Registered test accounts");

            // Get statistics for all accounts
            var statzJson = await controller.GetAccountStatzAsync();
            Console.WriteLine($"✓ Retrieved account statistics");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(statzJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("server_id", out var serverId))
            {
                Console.WriteLine($"  Server ID: {serverId.GetString()}");
            }

            if (root.TryGetProperty("now", out var now))
            {
                Console.WriteLine($"  Timestamp: {now.GetString()}");
            }

            if (root.TryGetProperty("accounts", out var accounts))
            {
                var accountsArray = accounts.EnumerateArray().ToList();
                Console.WriteLine($"  Number of accounts: {accountsArray.Count}");

                // Look for our test accounts
                var foundAccount1 = false;
                var foundAccount2 = false;

                foreach (var account in accountsArray)
                {
                    if (account.TryGetProperty("account", out var acctName))
                    {
                        var name = acctName.GetString();
                        if (name == "STATS_ACCOUNT_001") foundAccount1 = true;
                        if (name == "STATS_ACCOUNT_002") foundAccount2 = true;

                        if (name == "STATS_ACCOUNT_001" || name == "STATS_ACCOUNT_002")
                        {
                            Console.WriteLine($"    Found account: {name}");
                            if (account.TryGetProperty("conns", out var conns))
                            {
                                Console.WriteLine($"      Connections: {conns.GetInt32()}");
                            }
                            if (account.TryGetProperty("num_subscriptions", out var subs))
                            {
                                Console.WriteLine($"      Subscriptions: {subs.GetInt32()}");
                            }
                        }
                    }
                }

                if (!foundAccount1 || !foundAccount2)
                {
                    Console.WriteLine("❌ Test accounts not found in statistics");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("❌ Missing 'accounts' array in statistics response");
                return false;
            }

            Console.WriteLine("✓ AccountStatz test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AccountStatz test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    public static async Task<bool> TestAccountStatzWithFilter()
    {
        Console.WriteLine("\n=== Testing GetAccountStatz with Account Filter ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4237,
            Description = "Account statistics filter test"
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
            // Register a test account
            await controller.RegisterAccountAsync("FILTERED_ACCOUNT");
            Console.WriteLine($"✓ Registered test account");

            // Get statistics for specific account
            var statzJson = await controller.GetAccountStatzAsync("FILTERED_ACCOUNT");
            Console.WriteLine($"✓ Retrieved filtered account statistics");

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(statzJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("accounts", out var accounts))
            {
                var accountsArray = accounts.EnumerateArray().ToList();
                Console.WriteLine($"  Number of accounts in filtered response: {accountsArray.Count}");

                // Should only have the filtered account
                if (accountsArray.Count > 0)
                {
                    var firstAccount = accountsArray[0];
                    if (firstAccount.TryGetProperty("account", out var acctName))
                    {
                        var name = acctName.GetString();
                        Console.WriteLine($"    Account: {name}");

                        if (name != "FILTERED_ACCOUNT")
                        {
                            Console.WriteLine($"❌ Expected 'FILTERED_ACCOUNT', got '{name}'");
                            return false;
                        }
                    }
                }
            }

            Console.WriteLine("✓ AccountStatz with filter test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AccountStatz with filter test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }
}
