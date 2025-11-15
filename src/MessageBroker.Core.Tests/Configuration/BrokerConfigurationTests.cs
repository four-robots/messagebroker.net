using MessageBroker.Core.Configuration;
using Xunit;

namespace MessageBroker.Core.Tests.Configuration;

public class BrokerConfigurationTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new BrokerConfiguration();

        // Assert
        Assert.NotEqual(Guid.Empty, config.ConfigurationId);
        Assert.Equal("localhost", config.Host);
        Assert.Equal(4222, config.Port);
        Assert.Equal(1048576, config.MaxPayload);
        Assert.Equal(4096, config.MaxControlLine);
        Assert.Equal(120, config.PingInterval);
        Assert.Equal(2, config.MaxPingsOut);
        Assert.Equal(2, config.WriteDeadline);
        Assert.False(config.Debug);
        Assert.False(config.Trace);
        Assert.False(config.Jetstream);
        Assert.Equal("./jetstream", config.JetstreamStoreDir);
        Assert.Equal(-1, config.JetstreamMaxMemory);
        Assert.Equal(-1, config.JetstreamMaxStore);
        Assert.Equal(0, config.HttpPort);
        Assert.Equal("0.0.0.0", config.HttpHost);
        Assert.Equal(0, config.HttpsPort);
        Assert.NotNull(config.Auth);
        Assert.NotNull(config.LeafNode);
    }

    [Fact]
    public void Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new BrokerConfiguration
        {
            Host = "testhost",
            Port = 5000,
            Debug = true,
            Description = "Test config",
            Auth = new AuthConfiguration
            {
                Username = "user",
                Password = "pass"
            },
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                RemoteUrls = new List<string> { "nats://remote:7422" }
            }
        };

        // Act
        var clone = (BrokerConfiguration)original.Clone();

        // Assert
        Assert.NotEqual(original.ConfigurationId, clone.ConfigurationId);
        Assert.Equal(original.Host, clone.Host);
        Assert.Equal(original.Port, clone.Port);
        Assert.Equal(original.Debug, clone.Debug);
        Assert.Equal(original.Description, clone.Description);
        Assert.Equal(original.Auth.Username, clone.Auth.Username);
        Assert.Equal(original.Auth.Password, clone.Auth.Password);
        Assert.Equal(original.LeafNode.Port, clone.LeafNode.Port);
        Assert.Equal(original.LeafNode.RemoteUrls[0], clone.LeafNode.RemoteUrls[0]);

        // Verify deep copy - modifying clone shouldn't affect original
        clone.Host = "newhost";
        clone.Auth.Username = "newuser";
        clone.LeafNode.RemoteUrls.Add("nats://newremote:7422");

        Assert.NotEqual(original.Host, clone.Host);
        Assert.NotEqual(original.Auth.Username, clone.Auth.Username);
        Assert.Single(original.LeafNode.RemoteUrls);
        Assert.Equal(2, clone.LeafNode.RemoteUrls.Count);
    }

    [Fact]
    public void AuthConfiguration_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new AuthConfiguration
        {
            Username = "user",
            Password = "pass",
            Token = "token",
            AllowedUsers = new List<string> { "user1", "user2" }
        };

        // Act
        var clone = (AuthConfiguration)original.Clone();

        // Assert
        Assert.Equal(original.Username, clone.Username);
        Assert.Equal(original.Password, clone.Password);
        Assert.Equal(original.Token, clone.Token);
        Assert.Equal(original.AllowedUsers.Count, clone.AllowedUsers.Count);

        // Verify deep copy
        clone.AllowedUsers.Add("user3");
        Assert.Equal(2, original.AllowedUsers.Count);
        Assert.Equal(3, clone.AllowedUsers.Count);
    }

    [Fact]
    public void LeafNodeConfiguration_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new LeafNodeConfiguration
        {
            Host = "leafhost",
            Port = 7422,
            RemoteUrls = new List<string> { "nats://remote1:7422" },
            AuthUsername = "leafuser",
            AuthPassword = "leafpass",
            TlsCert = "/path/to/cert",
            TlsKey = "/path/to/key",
            TlsCaCert = "/path/to/ca",
            ImportSubjects = new List<string> { "import.>" },
            ExportSubjects = new List<string> { "export.>" }
        };

        // Act
        var clone = (LeafNodeConfiguration)original.Clone();

        // Assert
        Assert.Equal(original.Host, clone.Host);
        Assert.Equal(original.Port, clone.Port);
        Assert.Equal(original.RemoteUrls[0], clone.RemoteUrls[0]);
        Assert.Equal(original.AuthUsername, clone.AuthUsername);
        Assert.Equal(original.AuthPassword, clone.AuthPassword);
        Assert.Equal(original.TlsCert, clone.TlsCert);
        Assert.Equal(original.TlsKey, clone.TlsKey);
        Assert.Equal(original.TlsCaCert, clone.TlsCaCert);
        Assert.Equal(original.ImportSubjects[0], clone.ImportSubjects[0]);
        Assert.Equal(original.ExportSubjects[0], clone.ExportSubjects[0]);

        // Verify deep copy
        clone.RemoteUrls.Add("nats://remote2:7422");
        clone.ImportSubjects.Add("import2.>");
        clone.ExportSubjects.Add("export2.>");

        Assert.Single(original.RemoteUrls);
        Assert.Single(original.ImportSubjects);
        Assert.Single(original.ExportSubjects);
        Assert.Equal(2, clone.RemoteUrls.Count);
        Assert.Equal(2, clone.ImportSubjects.Count);
        Assert.Equal(2, clone.ExportSubjects.Count);
    }

    [Fact]
    public void ConfigurationId_IsUnique()
    {
        // Arrange & Act
        var config1 = new BrokerConfiguration();
        var config2 = new BrokerConfiguration();

        // Assert
        Assert.NotEqual(config1.ConfigurationId, config2.ConfigurationId);
    }

    [Fact]
    public void CreatedAt_IsSetToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var config = new BrokerConfiguration();
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(config.CreatedAt >= before);
        Assert.True(config.CreatedAt <= after);
    }
}
