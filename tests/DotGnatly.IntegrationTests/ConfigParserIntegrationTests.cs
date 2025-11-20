using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using DotGnatly.Core.Parsers;
using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Integration tests for NATS configuration parser with NatsController.
/// Tests actual config file parsing and application to running servers.
/// </summary>
public class ConfigParserIntegrationTests : IDisposable
{
    private readonly string _testConfigDir;
    private readonly List<NatsController> _controllers = new();

    public ConfigParserIntegrationTests()
    {
        // Create temporary directory for test config files
        _testConfigDir = Path.Combine(Path.GetTempPath(), $"nats-config-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testConfigDir);
    }

    public void Dispose()
    {
        // Cleanup controllers
        foreach (var controller in _controllers)
        {
            try
            {
                controller.ShutdownAsync().Wait(TimeSpan.FromSeconds(5));
                controller.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Cleanup test directory
        try
        {
            if (Directory.Exists(_testConfigDir))
            {
                Directory.Delete(_testConfigDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task ParseAndApply_BasicConfiguration_StartsSuccessfully()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:14222
server_name: ""test-basic""
monitor_port: 18222
jetstream: enabled
jetstream {
    store_dir=""/tmp/nats-test-basic""
    domain=test
}
max_payload: 8MB
";
        var configPath = Path.Combine(_testConfigDir, "basic.conf");
        File.WriteAllText(configPath, configContent);

        // Act
        var config = NatsConfigParser.ParseFile(configPath);
        var controller = new NatsController();
        _controllers.Add(controller);

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Equal(14222, config.Port);
        Assert.Equal("test-basic", config.ServerName);
        Assert.True(config.Jetstream);

        // Verify server is running
        var isRunning = await controller.IsServerRunningAsync(TestContext.Current.CancellationToken);
        Assert.True(isRunning);
    }

    [Fact]
    public async Task ParseAndApply_ConfigurationWithLogging_ConfiguresCorrectly()
    {
        // Arrange
        var logPath = Path.Combine(_testConfigDir, "test-server.log");
        var configContent = $@"
listen: 127.0.0.1:14223
server_name: ""test-logging""
monitor_port: 18223
debug: true
trace: false
log_file: ""{logPath}""
logfile_size_limit: 10MB
logfile_max_num: 5
max_payload: 4MB
write_deadline: 10s
";
        var configPath = Path.Combine(_testConfigDir, "logging.conf");
        File.WriteAllText(configPath, configContent);

        // Act
        var config = NatsConfigParser.ParseFile(configPath);
        var controller = new NatsController();
        _controllers.Add(controller);

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(config.Debug);
        Assert.False(config.Trace);
        Assert.Equal(logPath, config.LogFile);
        Assert.Equal(10 * 1024 * 1024, config.LogFileSize);
        Assert.Equal(5, config.LogFileMaxNum);
        Assert.Equal(10, config.WriteDeadline);
    }

    [Fact]
    public async Task ParseAndApply_JetStreamConfiguration_EnablesJetStream()
    {
        // Arrange
        var jsStoreDir = Path.Combine(_testConfigDir, "js-store");
        var configContent = $@"
listen: 127.0.0.1:14224
server_name: ""test-jetstream""
monitor_port: 18224
jetstream: enabled
jetstream {{
    store_dir=""{jsStoreDir}""
    domain=""integration-test""
    max_memory=512MB
    max_file=2GB
    unique_tag=""test-server-01""
}}
";
        var configPath = Path.Combine(_testConfigDir, "jetstream.conf");
        File.WriteAllText(configPath, configContent);

        // Act
        var config = NatsConfigParser.ParseFile(configPath);
        var controller = new NatsController();
        _controllers.Add(controller);

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.True(config.Jetstream);
        Assert.Equal(jsStoreDir, config.JetstreamStoreDir);
        Assert.Equal("integration-test", config.JetstreamDomain);
        Assert.Equal(512L * 1024 * 1024, config.JetstreamMaxMemory);
        Assert.Equal(2L * 1024 * 1024 * 1024, config.JetstreamMaxStore);
        Assert.Equal("test-server-01", config.JetstreamUniqueTag);

        // Verify JetStream is enabled
        var jsEnabled = await controller.IsJetStreamEnabledAsync(TestContext.Current.CancellationToken);
        Assert.True(jsEnabled);
    }

    [Fact]
    public async Task ParseAndApply_LeafNodeConfiguration_ConfiguresLeafNode()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:14225
server_name: ""test-leafnode""
monitor_port: 18225
leafnodes {
    host: 0.0.0.0
    port: 17422
}
";
        var configPath = Path.Combine(_testConfigDir, "leafnode.conf");
        File.WriteAllText(configPath, configContent);

        // Act
        var config = NatsConfigParser.ParseFile(configPath);
        var controller = new NatsController();
        _controllers.Add(controller);

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("0.0.0.0", config.LeafNode.Host);
        Assert.Equal(17422, config.LeafNode.Port);
    }

    [Fact]
    public async Task ParseAndApply_CompleteConfiguration_AppliesAllSettings()
    {
        // Arrange
        var jsStoreDir = Path.Combine(_testConfigDir, "complete-js");
        var logPath = Path.Combine(_testConfigDir, "complete.log");
        var configContent = $@"
listen: 127.0.0.1:14226
server_name: ""test-complete""
monitor_port: 18226
debug: true
trace: false
log_file: ""{logPath}""
logfile_size_limit: 100MB
logfile_max_num: 10
max_payload: 16MB
write_deadline: 20s
disable_sublist_cache: false
system_account: SYS
jetstream: enabled
jetstream {{
    store_dir=""{jsStoreDir}""
    domain=""complete-test""
    max_memory=1GB
    max_file=10GB
    unique_tag=""complete-01""
}}
leafnodes {{
    host: 127.0.0.1
    port: 17423
}}
";
        var configPath = Path.Combine(_testConfigDir, "complete.conf");
        File.WriteAllText(configPath, configContent);

        // Act
        var config = NatsConfigParser.ParseFile(configPath);
        var controller = new NatsController();
        _controllers.Add(controller);

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);

        // Verify all settings
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Equal(14226, config.Port);
        Assert.Equal("test-complete", config.ServerName);
        Assert.Equal(18226, config.HttpPort);
        Assert.True(config.Debug);
        Assert.False(config.Trace);
        Assert.Equal(logPath, config.LogFile);
        Assert.Equal(100L * 1024 * 1024, config.LogFileSize);
        Assert.Equal(10, config.LogFileMaxNum);
        Assert.Equal(16 * 1024 * 1024, config.MaxPayload);
        Assert.Equal(20, config.WriteDeadline);
        Assert.False(config.DisableSublistCache);
        Assert.Equal("SYS", config.SystemAccount);

        // JetStream settings
        Assert.True(config.Jetstream);
        Assert.Equal(jsStoreDir, config.JetstreamStoreDir);
        Assert.Equal("complete-test", config.JetstreamDomain);
        Assert.Equal(1L * 1024 * 1024 * 1024, config.JetstreamMaxMemory);
        Assert.Equal(10L * 1024 * 1024 * 1024, config.JetstreamMaxStore);
        Assert.Equal("complete-01", config.JetstreamUniqueTag);

        // Leaf node settings
        Assert.Equal("127.0.0.1", config.LeafNode.Host);
        Assert.Equal(17423, config.LeafNode.Port);

        // Verify server is running
        var isRunning = await controller.IsServerRunningAsync(TestContext.Current.CancellationToken);
        Assert.True(isRunning);

        // Verify JetStream is enabled
        var jsEnabled = await controller.IsJetStreamEnabledAsync(TestContext.Current.CancellationToken);
        Assert.True(jsEnabled);
    }

    [Fact]
    public async Task ParseAndApply_WithValidation_PassesValidation()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:14227
server_name: ""test-validation""
monitor_port: 18227
max_payload: 8MB
write_deadline: 10s
jetstream: enabled
jetstream {
    store_dir=""/tmp/nats-validation-test""
}
";
        var configPath = Path.Combine(_testConfigDir, "validation.conf");
        File.WriteAllText(configPath, configContent);

        // Act
        var config = NatsConfigParser.ParseFile(configPath);

        // Validate configuration
        var validator = new DotGnatly.Core.Validation.ConfigurationValidator();
        var validationResult = validator.Validate(config);

        // Assert validation
        Assert.True(validationResult.IsValid, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        // Apply configuration
        var controller = new NatsController();
        _controllers.Add(controller);

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public async Task ParseFile_ActualBasicConfigFile_ParsesAndAppliesCorrectly()
    {
        // Arrange
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            // Skip test if we can't find the repository root
            return;
        }

        var configPath = Path.Combine(repoRoot, "test-configs", "basic.conf");
        if (!File.Exists(configPath))
        {
            // Skip if config file doesn't exist
            return;
        }

        // Act
        var config = NatsConfigParser.ParseFile(configPath);

        // Assert - verify basic config properties
        Assert.NotNull(config);
        Assert.Equal("nats-server-01", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.True(config.Jetstream);
        Assert.Equal("basic", config.JetstreamDomain);
        Assert.Equal("SYS", config.SystemAccount);

        // Note: We don't actually start the server for the repository config files
        // as they may have conflicting ports with running tests
    }

    [Fact]
    public async Task ParseFile_ActualLeafConfigFile_ParsesCorrectly()
    {
        // Arrange
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            return;
        }

        var configPath = Path.Combine(repoRoot, "test-configs", "leaf.conf");
        if (!File.Exists(configPath))
        {
            return;
        }

        // Act
        var config = NatsConfigParser.ParseFile(configPath);

        // Assert - Basic properties
        Assert.NotNull(config);
        Assert.Equal("nats-leaf-01", config.ServerName);
        Assert.Equal(4223, config.Port);
        Assert.Equal(8223, config.HttpPort);
        Assert.True(config.Jetstream);
        Assert.Equal("leaf-01", config.JetstreamDomain);
        Assert.Equal(100 * 1024 * 1024, config.LogFileSize);
        Assert.Equal(10, config.LogFileMaxNum);
        Assert.Equal(10, config.WriteDeadline);
        Assert.Equal("SYS", config.SystemAccount);

        // Assert - Leaf node advanced features
        Assert.True(config.LeafNode.IsolateLeafnodeInterest);
        Assert.Equal("2s", config.LeafNode.ReconnectDelay);

        // Assert - Leaf node TLS with pinned certs
        Assert.NotNull(config.LeafNode.Tls);
        Assert.Equal(2, config.LeafNode.Tls.PinnedCerts.Count);
        Assert.Equal(60, config.LeafNode.Tls.Timeout);

        // Assert - Leaf node remotes
        Assert.Single(config.LeafNode.Remotes);
        var remote = config.LeafNode.Remotes[0];
        Assert.Single(remote.Urls);
        Assert.Contains("tls://", remote.Urls[0]);
        Assert.Equal("LEAF-DMZ", remote.Account);
        Assert.NotNull(remote.Tls);
        Assert.True(remote.Tls.HandshakeFirst);
        Assert.True(remote.Tls.Insecure);
        Assert.Equal(60, remote.Tls.Timeout);

        // Assert - Accounts
        Assert.Equal(3, config.Accounts.Count);
        var sysAccount = config.Accounts.FirstOrDefault(a => a.Name == "SYS");
        var appAccount = config.Accounts.FirstOrDefault(a => a.Name == "APP");
        var leafDmzAccount = config.Accounts.FirstOrDefault(a => a.Name == "LEAF-DMZ");

        Assert.NotNull(sysAccount);
        Assert.NotNull(appAccount);
        Assert.NotNull(leafDmzAccount);

        // Assert - SYS account
        Assert.Single(sysAccount.Users);
        Assert.Equal("admin", sysAccount.Users[0].User);
        Assert.Equal(3, sysAccount.Exports.Count);

        // Assert - APP account
        Assert.True(appAccount.Jetstream);
        Assert.Single(appAccount.Users);
        Assert.Equal("app-user", appAccount.Users[0].User);
        Assert.True(appAccount.Imports.Count >= 14); // Has 14 imports
        Assert.True(appAccount.Exports.Count > 20); // Has many exports
        Assert.Single(appAccount.Mappings); // Has one mapping

        // Assert - APP account imports with "to" mapping
        var importWithTo = appAccount.Imports.FirstOrDefault(i => i.To != null);
        Assert.NotNull(importWithTo);
        Assert.NotNull(importWithTo.To);

        // Assert - APP account exports with response_type
        var exportWithResponseType = appAccount.Exports.FirstOrDefault(e => e.ResponseType != null);
        Assert.NotNull(exportWithResponseType);

        // Assert - LEAF-DMZ account
        Assert.False(leafDmzAccount.Jetstream); // jetstream: disabled
        Assert.True(leafDmzAccount.Exports.Count > 10);
        Assert.True(leafDmzAccount.Imports.Count > 15);
    }

    [Fact]
    public async Task ParseFile_ActualHubConfigFile_ParsesCorrectly()
    {
        // Arrange
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            return;
        }

        var configPath = Path.Combine(repoRoot, "test-configs", "hub.conf");
        if (!File.Exists(configPath))
        {
            return;
        }

        // Act
        var config = NatsConfigParser.ParseFile(configPath);

        // Assert - Basic properties
        Assert.NotNull(config);
        Assert.Equal("nats-hub", config.ServerName);
        Assert.Equal(4222, config.Port);
        Assert.Equal(8222, config.HttpPort);
        Assert.True(config.Jetstream);
        Assert.Equal("hub", config.JetstreamDomain);
        Assert.Equal(7422, config.LeafNode.Port);
        Assert.False(config.DisableSublistCache);
        Assert.Equal("SYS", config.SystemAccount);

        // Assert - Leaf node advanced features
        Assert.True(config.LeafNode.IsolateLeafnodeInterest);
        Assert.Equal("10.0.0.21:7422", config.LeafNode.Advertise);

        // Assert - Leaf node TLS with Windows cert store
        Assert.NotNull(config.LeafNode.Tls);
        Assert.Equal("WindowsLocalMachine", config.LeafNode.Tls.CertStore);
        Assert.Equal("Subject", config.LeafNode.Tls.CertMatchBy);
        Assert.Equal("nats-cert-hub", config.LeafNode.Tls.CertMatch);
        Assert.True(config.LeafNode.Tls.HandshakeFirst);
        Assert.Equal(60, config.LeafNode.Tls.Timeout);

        // Assert - Leaf node authorization
        Assert.NotNull(config.LeafNode.Authorization);
        Assert.Equal("app-user", config.LeafNode.Authorization.User);
        Assert.NotNull(config.LeafNode.Authorization.Password);
        Assert.Equal("HUB-DMZ", config.LeafNode.Authorization.Account);
        Assert.Equal(60, config.LeafNode.Authorization.Timeout);

        // Assert - Accounts
        Assert.Equal(3, config.Accounts.Count);
        var sysAccount = config.Accounts.FirstOrDefault(a => a.Name == "SYS");
        var appAccount = config.Accounts.FirstOrDefault(a => a.Name == "APP");
        var hubDmzAccount = config.Accounts.FirstOrDefault(a => a.Name == "HUB-DMZ");

        Assert.NotNull(sysAccount);
        Assert.NotNull(appAccount);
        Assert.NotNull(hubDmzAccount);

        // Assert - SYS account
        Assert.Single(sysAccount.Users);
        Assert.Equal("admin", sysAccount.Users[0].User);
        Assert.Equal(3, sysAccount.Exports.Count);

        // Assert - APP account
        Assert.True(appAccount.Jetstream);
        Assert.Single(appAccount.Users);
        Assert.Equal("app-user", appAccount.Users[0].User);
        Assert.True(appAccount.Imports.Count > 20); // Has many imports
        Assert.True(appAccount.Exports.Count > 10); // Has many exports

        // Assert - APP account imports with "to" mapping
        var importWithTo = appAccount.Imports.FirstOrDefault(i => i.To != null);
        Assert.NotNull(importWithTo);
        Assert.NotNull(importWithTo.To);

        // Assert - HUB-DMZ account
        Assert.False(hubDmzAccount.Jetstream); // jetstream: disabled
        Assert.True(hubDmzAccount.Exports.Count > 20);
        Assert.True(hubDmzAccount.Imports.Count > 10);

        // Assert - HUB-DMZ account exports with response_type and response_threshold
        var exportWithResponseType = hubDmzAccount.Exports.FirstOrDefault(e => e.ResponseType != null);
        Assert.NotNull(exportWithResponseType);
        var exportWithResponseThreshold = hubDmzAccount.Exports.FirstOrDefault(e => e.ResponseThreshold != null);
        Assert.NotNull(exportWithResponseThreshold);
    }

    private string? FindRepositoryRoot()
    {
        var directory = Directory.GetCurrentDirectory();
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory, "test-configs")))
            {
                return directory;
            }
            directory = Directory.GetParent(directory)?.FullName;
        }
        return null;
    }
}
