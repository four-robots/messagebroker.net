using System.Text.Json;
using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Integration tests for NATS server account management (RegisterAccount, LookupAccount, AccountStatz).
/// </summary>
public class AccountManagementTests
{
    [Fact]
    public async Task TestRegisterAccount()
    {
        using var controller = new NatsController();

        // Start server
        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4233,
            Description = "Account registration test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Register a new account
            var accountJson = await controller.RegisterAccountAsync("TEST_ACCOUNT_001", TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(accountJson);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("account", out var accountName));
            Assert.Equal("TEST_ACCOUNT_001", accountName.GetString());

            // Test duplicate registration (should fail)
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await controller.RegisterAccountAsync("TEST_ACCOUNT_001", TestContext.Current.CancellationToken);
            });
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestLookupAccount()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4234,
            Description = "Account lookup test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Register an account first
            var registerJson = await controller.RegisterAccountAsync("TEST_LOOKUP_ACCOUNT", TestContext.Current.CancellationToken);

            // Now look it up
            var lookupJson = await controller.LookupAccountAsync("TEST_LOOKUP_ACCOUNT", TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(lookupJson);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("account", out var accountName));
            Assert.Equal("TEST_LOOKUP_ACCOUNT", accountName.GetString());
            Assert.True(root.TryGetProperty("connections", out _));
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestLookupNonExistentAccount()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4235,
            Description = "Account lookup error test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Try to lookup non-existent account (should throw)
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await controller.LookupAccountAsync("NONEXISTENT_ACCOUNT", TestContext.Current.CancellationToken);
            });
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestAccountStatz()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4236,
            Description = "Account statistics test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Register some test accounts
            await controller.RegisterAccountAsync("STATS_ACCOUNT_001", TestContext.Current.CancellationToken);
            await controller.RegisterAccountAsync("STATS_ACCOUNT_002", TestContext.Current.CancellationToken);

            // Get statistics for all accounts
            var statzJson = await controller.GetAccountStatzAsync(cancellationToken: TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(statzJson);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("server_id", out _));
            Assert.True(root.TryGetProperty("now", out _));

            // In NATS 2.12+, the field is named "account_statz" (not "accounts")
            Assert.True(root.TryGetProperty("account_statz", out var accounts));

            var accountsArray = accounts.EnumerateArray().ToList();
            var foundAccount1 = accountsArray.Any(a => a.TryGetProperty("acc", out var name) && name.GetString() == "STATS_ACCOUNT_001");
            var foundAccount2 = accountsArray.Any(a => a.TryGetProperty("acc", out var name) && name.GetString() == "STATS_ACCOUNT_002");

            Assert.True(foundAccount1, "STATS_ACCOUNT_001 not found in statistics");
            Assert.True(foundAccount2, "STATS_ACCOUNT_002 not found in statistics");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task TestAccountStatzWithFilter()
    {
        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4237,
            Description = "Account statistics filter test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Register a test account
            await controller.RegisterAccountAsync("FILTERED_ACCOUNT", TestContext.Current.CancellationToken);

            // Get statistics for specific account
            var statzJson = await controller.GetAccountStatzAsync("FILTERED_ACCOUNT", TestContext.Current.CancellationToken);

            // Parse and validate JSON
            using var doc = JsonDocument.Parse(statzJson);
            var root = doc.RootElement;

            // In NATS 2.12+, the field is named "account_statz" (not "accounts")
            Assert.True(root.TryGetProperty("account_statz", out var accounts));

            var accountsArray = accounts.EnumerateArray().ToList();
            Assert.True(accountsArray.Count > 0, "No accounts in filtered response");

            var firstAccount = accountsArray[0];
            Assert.True(firstAccount.TryGetProperty("acc", out var acctName));
            Assert.Equal("FILTERED_ACCOUNT", acctName.GetString());
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }
}
