using System;
using System.IO;
using Xunit;
using DotGnatly.Core.Parsers;

namespace DotGnatly.Core.Tests.Parsers;

/// <summary>
/// Tests that validate the actual configuration files in test-configs/ directory.
/// These tests ensure the example config files are valid and parse correctly.
/// </summary>
public class ActualConfigFilesTests
{
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

    [Fact]
    public void Parse_BasicConfFile_ParsesAllProperties()
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

        // Assert - Verify all expected properties from basic.conf
        Assert.NotNull(config);
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Equal(4222, config.Port);
        Assert.Equal("nats-server-01", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.True(config.Jetstream);
        Assert.Equal("/var/lib/nats/data-store", config.JetstreamStoreDir);
        Assert.Equal("basic", config.JetstreamDomain);
        Assert.Equal("/var/log/nats/nats-server-01.log", config.LogFile);
        Assert.Equal(8 * 1024 * 1024, config.MaxPayload);
        Assert.Equal("SYS", config.SystemAccount);
    }

    [Fact]
    public void Parse_LeafConfFile_ParsesAllProperties()
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

        // Assert - Verify all expected properties from leaf.conf
        Assert.NotNull(config);
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Equal(4223, config.Port);
        Assert.Equal("nats-leaf-01", config.ServerName);
        Assert.Equal(8223, config.HttpPort);
        Assert.True(config.Jetstream);
        Assert.Equal("/var/lib/nats/leaf-data", config.JetstreamStoreDir);
        Assert.Equal("leaf-01", config.JetstreamDomain);
        Assert.False(config.Debug);
        Assert.False(config.Trace);
        Assert.Equal("/var/log/nats/nats-leaf-01.log", config.LogFile);
        Assert.Equal(100 * 1024 * 1024, config.LogFileSize);
        Assert.Equal(10, config.LogFileMaxNum);
        Assert.Equal(8 * 1024 * 1024, config.MaxPayload);
        Assert.Equal(10, config.WriteDeadline);
        Assert.Equal("SYS", config.SystemAccount);
    }

    [Fact]
    public void Parse_HubConfFile_ParsesAllProperties()
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

        // Assert - Verify all expected properties from hub.conf
        Assert.NotNull(config);
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Equal(4222, config.Port);
        Assert.Equal("nats-hub", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.True(config.Jetstream);
        Assert.Equal("/var/lib/nats/hub-data", config.JetstreamStoreDir);
        Assert.Equal("hub", config.JetstreamDomain);
        Assert.False(config.Debug);
        Assert.False(config.Trace);
        Assert.Equal("/var/log/nats/nats-hub.log", config.LogFile);
        Assert.Equal(100 * 1024 * 1024, config.LogFileSize);
        Assert.Equal(10, config.LogFileMaxNum);
        Assert.False(config.DisableSublistCache);
        Assert.Equal(8 * 1024 * 1024, config.MaxPayload);
        Assert.Equal(10, config.WriteDeadline);
        Assert.Equal("SYS", config.SystemAccount);

        // Leaf node configuration
        Assert.Equal(7422, config.LeafNode.Port);
    }

    [Fact]
    public void Parse_AllConfigFiles_AreValid()
    {
        // Arrange
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            return;
        }

        var testConfigsDir = Path.Combine(repoRoot, "test-configs");
        if (!Directory.Exists(testConfigsDir))
        {
            return;
        }

        var configFiles = Directory.GetFiles(testConfigsDir, "*.conf");

        // Act & Assert
        foreach (var configFile in configFiles)
        {
            // Should not throw
            var config = NatsConfigParser.ParseFile(configFile);
            Assert.NotNull(config);
        }
    }

    [Fact]
    public void Parse_BasicConfFile_CanSerializeToJson()
    {
        // Arrange
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            return;
        }

        var configPath = Path.Combine(repoRoot, "test-configs", "basic.conf");
        if (!File.Exists(configPath))
        {
            return;
        }

        // Act
        var config = NatsConfigParser.ParseFile(configPath);
        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        // Assert
        Assert.NotNull(json);
        Assert.Contains("nats-server-01", json);
        Assert.Contains("basic", json);
    }

    [Fact]
    public void Parse_LeafConfFile_HasExpectedLeafNodeConfig()
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

        // Assert - Leaf node specific checks
        Assert.NotNull(config);
        Assert.NotNull(config.LeafNode);
        // Default host should be set
        Assert.NotNull(config.LeafNode.Host);
    }

    [Fact]
    public void Parse_HubConfFile_HasLeafNodePort()
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

        // Assert - Hub should have leaf node port configured
        Assert.NotNull(config);
        Assert.NotNull(config.LeafNode);
        Assert.Equal(7422, config.LeafNode.Port);
    }

    [Fact]
    public void Parse_AllConfigFiles_HaveUniqueServerNames()
    {
        // Arrange
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            return;
        }

        var testConfigsDir = Path.Combine(repoRoot, "test-configs");
        if (!Directory.Exists(testConfigsDir))
        {
            return;
        }

        var configFiles = Directory.GetFiles(testConfigsDir, "*.conf");
        var serverNames = new HashSet<string>();

        // Act
        foreach (var configFile in configFiles)
        {
            var config = NatsConfigParser.ParseFile(configFile);
            if (!string.IsNullOrEmpty(config.ServerName))
            {
                serverNames.Add(config.ServerName);
            }
        }

        // Assert - All server names should be unique
        Assert.Equal(configFiles.Length, serverNames.Count);
    }

    [Fact]
    public void Parse_AllConfigFiles_HaveValidPorts()
    {
        // Arrange
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            return;
        }

        var testConfigsDir = Path.Combine(repoRoot, "test-configs");
        if (!Directory.Exists(testConfigsDir))
        {
            return;
        }

        var configFiles = Directory.GetFiles(testConfigsDir, "*.conf");

        // Act & Assert
        foreach (var configFile in configFiles)
        {
            var config = NatsConfigParser.ParseFile(configFile);

            // Port should be in valid range
            Assert.InRange(config.Port, 1, 65535);

            // Monitor port should be valid if set
            if (config.HttpPort > 0)
            {
                Assert.InRange(config.HttpPort, 1, 65535);
            }
        }
    }

    [Fact]
    public void Parse_AllConfigFiles_HaveSystemAccount()
    {
        // Arrange
        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            return;
        }

        var testConfigsDir = Path.Combine(repoRoot, "test-configs");
        if (!Directory.Exists(testConfigsDir))
        {
            return;
        }

        var configFiles = Directory.GetFiles(testConfigsDir, "*.conf");

        // Act & Assert
        foreach (var configFile in configFiles)
        {
            var config = NatsConfigParser.ParseFile(configFile);

            // All config files should have a system account configured
            Assert.NotNull(config.SystemAccount);
            Assert.NotEmpty(config.SystemAccount);
        }
    }
}
