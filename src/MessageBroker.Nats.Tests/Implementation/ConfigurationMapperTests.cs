using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Bindings;
using MessageBroker.Nats.Implementation;
using Xunit;

namespace MessageBroker.Nats.Tests.Implementation;

public class ConfigurationMapperTests
{
    [Fact]
    public void MapToServerConfig_NullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ConfigurationMapper.MapToServerConfig(null!));
    }

    [Fact]
    public void MapToBrokerConfiguration_NullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ConfigurationMapper.MapToBrokerConfiguration(null!));
    }

    [Fact]
    public void MapToServerConfig_BasicProperties_MapsCorrectly()
    {
        // Arrange
        var brokerConfig = new BrokerConfiguration
        {
            Host = "testhost",
            Port = 5000,
            MaxPayload = 2048576,
            MaxControlLine = 8192,
            PingInterval = 60,
            MaxPingsOut = 3,
            WriteDeadline = 5,
            Debug = true,
            Trace = true
        };

        // Act
        var serverConfig = ConfigurationMapper.MapToServerConfig(brokerConfig);

        // Assert
        Assert.Equal("testhost", serverConfig.Host);
        Assert.Equal(5000, serverConfig.Port);
        Assert.Equal(2048576, serverConfig.MaxPayload);
        Assert.Equal(8192, serverConfig.MaxControlLine);
        Assert.Equal(60, serverConfig.PingInterval);
        Assert.Equal(3, serverConfig.MaxPingsOut);
        Assert.Equal(5, serverConfig.WriteDeadline);
        Assert.True(serverConfig.Debug);
        Assert.True(serverConfig.Trace);
    }

    [Fact]
    public void MapToServerConfig_JetStreamSettings_MapsCorrectly()
    {
        // Arrange
        var brokerConfig = new BrokerConfiguration
        {
            Jetstream = true,
            JetstreamStoreDir = "/data/jetstream",
            JetstreamMaxMemory = 1073741824,
            JetstreamMaxStore = 10737418240
        };

        // Act
        var serverConfig = ConfigurationMapper.MapToServerConfig(brokerConfig);

        // Assert
        Assert.True(serverConfig.Jetstream);
        Assert.Equal("/data/jetstream", serverConfig.JetstreamStoreDir);
        Assert.Equal(1073741824, serverConfig.JetstreamMaxMemory);
        Assert.Equal(10737418240, serverConfig.JetstreamMaxStore);
    }

    [Fact]
    public void MapToServerConfig_HttpSettings_MapsCorrectly()
    {
        // Arrange
        var brokerConfig = new BrokerConfiguration
        {
            HttpPort = 8080,
            HttpHost = "localhost",
            HttpsPort = 8443
        };

        // Act
        var serverConfig = ConfigurationMapper.MapToServerConfig(brokerConfig);

        // Assert
        Assert.Equal(8080, serverConfig.HTTPPort);
        Assert.Equal("localhost", serverConfig.HTTPHost);
        Assert.Equal(8443, serverConfig.HTTPSPort);
    }

    [Fact]
    public void MapToServerConfig_AuthConfiguration_MapsCorrectly()
    {
        // Arrange
        var brokerConfig = new BrokerConfiguration
        {
            Auth = new AuthConfiguration
            {
                Username = "testuser",
                Password = "testpass",
                Token = "testtoken",
                AllowedUsers = new List<string> { "user1", "user2" }
            }
        };

        // Act
        var serverConfig = ConfigurationMapper.MapToServerConfig(brokerConfig);

        // Assert
        Assert.NotNull(serverConfig.Auth);
        Assert.Equal("testuser", serverConfig.Auth.Username);
        Assert.Equal("testpass", serverConfig.Auth.Password);
        Assert.Equal("testtoken", serverConfig.Auth.Token);
        Assert.Equal(2, serverConfig.Auth.AllowedUsers.Count);
        Assert.Contains("user1", serverConfig.Auth.AllowedUsers);
        Assert.Contains("user2", serverConfig.Auth.AllowedUsers);
    }

    [Fact]
    public void MapToServerConfig_LeafNodeConfiguration_MapsCorrectly()
    {
        // Arrange
        var brokerConfig = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                Host = "leafhost",
                Port = 7422,
                RemoteUrls = new List<string> { "nats://remote1:7422", "nats://remote2:7422" },
                AuthUsername = "leafuser",
                AuthPassword = "leafpass",
                TlsCert = "/path/to/cert",
                TlsKey = "/path/to/key",
                TlsCaCert = "/path/to/ca",
                ImportSubjects = new List<string> { "import.>" },
                ExportSubjects = new List<string> { "export.>" }
            }
        };

        // Act
        var serverConfig = ConfigurationMapper.MapToServerConfig(brokerConfig);

        // Assert
        Assert.NotNull(serverConfig.LeafNode);
        Assert.Equal("leafhost", serverConfig.LeafNode.Host);
        Assert.Equal(7422, serverConfig.LeafNode.Port);
        Assert.Equal(2, serverConfig.LeafNode.RemoteURLs.Count);
        Assert.Contains("nats://remote1:7422", serverConfig.LeafNode.RemoteURLs);
        Assert.Contains("nats://remote2:7422", serverConfig.LeafNode.RemoteURLs);
        Assert.Equal("leafuser", serverConfig.LeafNode.AuthUsername);
        Assert.Equal("leafpass", serverConfig.LeafNode.AuthPassword);
        Assert.Equal("/path/to/cert", serverConfig.LeafNode.TLSCert);
        Assert.Equal("/path/to/key", serverConfig.LeafNode.TLSKey);
        Assert.Equal("/path/to/ca", serverConfig.LeafNode.TLSCACert);
        Assert.Single(serverConfig.LeafNode.ImportSubjects);
        Assert.Contains("import.>", serverConfig.LeafNode.ImportSubjects);
        Assert.Single(serverConfig.LeafNode.ExportSubjects);
        Assert.Contains("export.>", serverConfig.LeafNode.ExportSubjects);
    }

    [Fact]
    public void MapToBrokerConfiguration_BasicProperties_MapsCorrectly()
    {
        // Arrange
        var serverConfig = new ServerConfig
        {
            Host = "testhost",
            Port = 5000,
            MaxPayload = 2048576,
            MaxControlLine = 8192,
            PingInterval = 60,
            MaxPingsOut = 3,
            WriteDeadline = 5,
            Debug = true,
            Trace = true
        };

        // Act
        var brokerConfig = ConfigurationMapper.MapToBrokerConfiguration(serverConfig);

        // Assert
        Assert.Equal("testhost", brokerConfig.Host);
        Assert.Equal(5000, brokerConfig.Port);
        Assert.Equal(2048576, brokerConfig.MaxPayload);
        Assert.Equal(8192, brokerConfig.MaxControlLine);
        Assert.Equal(60, brokerConfig.PingInterval);
        Assert.Equal(3, brokerConfig.MaxPingsOut);
        Assert.Equal(5, brokerConfig.WriteDeadline);
        Assert.True(brokerConfig.Debug);
        Assert.True(brokerConfig.Trace);
    }

    [Fact]
    public void MapToBrokerConfiguration_JetStreamSettings_MapsCorrectly()
    {
        // Arrange
        var serverConfig = new ServerConfig
        {
            Jetstream = true,
            JetstreamStoreDir = "/data/jetstream",
            JetstreamMaxMemory = 1073741824,
            JetstreamMaxStore = 10737418240
        };

        // Act
        var brokerConfig = ConfigurationMapper.MapToBrokerConfiguration(serverConfig);

        // Assert
        Assert.True(brokerConfig.Jetstream);
        Assert.Equal("/data/jetstream", brokerConfig.JetstreamStoreDir);
        Assert.Equal(1073741824, brokerConfig.JetstreamMaxMemory);
        Assert.Equal(10737418240, brokerConfig.JetstreamMaxStore);
    }

    [Fact]
    public void MapToBrokerConfiguration_HttpSettings_MapsCorrectly()
    {
        // Arrange
        var serverConfig = new ServerConfig
        {
            HTTPPort = 8080,
            HTTPHost = "localhost",
            HTTPSPort = 8443
        };

        // Act
        var brokerConfig = ConfigurationMapper.MapToBrokerConfiguration(serverConfig);

        // Assert
        Assert.Equal(8080, brokerConfig.HttpPort);
        Assert.Equal("localhost", brokerConfig.HttpHost);
        Assert.Equal(8443, brokerConfig.HttpsPort);
    }

    [Fact]
    public void MapToBrokerConfiguration_AuthConfiguration_MapsCorrectly()
    {
        // Arrange
        var serverConfig = new ServerConfig
        {
            Auth = new AuthConfig
            {
                Username = "testuser",
                Password = "testpass",
                Token = "testtoken",
                AllowedUsers = new List<string> { "user1", "user2" }
            }
        };

        // Act
        var brokerConfig = ConfigurationMapper.MapToBrokerConfiguration(serverConfig);

        // Assert
        Assert.NotNull(brokerConfig.Auth);
        Assert.Equal("testuser", brokerConfig.Auth.Username);
        Assert.Equal("testpass", brokerConfig.Auth.Password);
        Assert.Equal("testtoken", brokerConfig.Auth.Token);
        Assert.Equal(2, brokerConfig.Auth.AllowedUsers.Count);
        Assert.Contains("user1", brokerConfig.Auth.AllowedUsers);
        Assert.Contains("user2", brokerConfig.Auth.AllowedUsers);
    }

    [Fact]
    public void MapToBrokerConfiguration_LeafNodeConfiguration_MapsCorrectly()
    {
        // Arrange
        var serverConfig = new ServerConfig
        {
            LeafNode = new LeafNodeConfig
            {
                Host = "leafhost",
                Port = 7422,
                RemoteURLs = new List<string> { "nats://remote1:7422", "nats://remote2:7422" },
                AuthUsername = "leafuser",
                AuthPassword = "leafpass",
                TLSCert = "/path/to/cert",
                TLSKey = "/path/to/key",
                TLSCACert = "/path/to/ca",
                ImportSubjects = new List<string> { "import.>" },
                ExportSubjects = new List<string> { "export.>" }
            }
        };

        // Act
        var brokerConfig = ConfigurationMapper.MapToBrokerConfiguration(serverConfig);

        // Assert
        Assert.NotNull(brokerConfig.LeafNode);
        Assert.Equal("leafhost", brokerConfig.LeafNode.Host);
        Assert.Equal(7422, brokerConfig.LeafNode.Port);
        Assert.Equal(2, brokerConfig.LeafNode.RemoteUrls.Count);
        Assert.Contains("nats://remote1:7422", brokerConfig.LeafNode.RemoteUrls);
        Assert.Contains("nats://remote2:7422", brokerConfig.LeafNode.RemoteUrls);
        Assert.Equal("leafuser", brokerConfig.LeafNode.AuthUsername);
        Assert.Equal("leafpass", brokerConfig.LeafNode.AuthPassword);
        Assert.Equal("/path/to/cert", brokerConfig.LeafNode.TlsCert);
        Assert.Equal("/path/to/key", brokerConfig.LeafNode.TlsKey);
        Assert.Equal("/path/to/ca", brokerConfig.LeafNode.TlsCaCert);
        Assert.Single(brokerConfig.LeafNode.ImportSubjects);
        Assert.Contains("import.>", brokerConfig.LeafNode.ImportSubjects);
        Assert.Single(brokerConfig.LeafNode.ExportSubjects);
        Assert.Contains("export.>", brokerConfig.LeafNode.ExportSubjects);
    }

    [Fact]
    public void RoundTrip_BrokerToServerToBroker_PreservesValues()
    {
        // Arrange
        var original = new BrokerConfiguration
        {
            Host = "testhost",
            Port = 5000,
            Debug = true,
            Jetstream = true,
            JetstreamStoreDir = "/data",
            HttpPort = 8080,
            Auth = new AuthConfiguration
            {
                Username = "user",
                Password = "pass",
                AllowedUsers = new List<string> { "user1" }
            },
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                RemoteUrls = new List<string> { "nats://remote:7422" },
                ImportSubjects = new List<string> { "import.>" }
            }
        };

        // Act
        var serverConfig = ConfigurationMapper.MapToServerConfig(original);
        var roundTripped = ConfigurationMapper.MapToBrokerConfiguration(serverConfig);

        // Assert
        Assert.Equal(original.Host, roundTripped.Host);
        Assert.Equal(original.Port, roundTripped.Port);
        Assert.Equal(original.Debug, roundTripped.Debug);
        Assert.Equal(original.Jetstream, roundTripped.Jetstream);
        Assert.Equal(original.JetstreamStoreDir, roundTripped.JetstreamStoreDir);
        Assert.Equal(original.HttpPort, roundTripped.HttpPort);
        Assert.Equal(original.Auth.Username, roundTripped.Auth.Username);
        Assert.Equal(original.Auth.Password, roundTripped.Auth.Password);
        Assert.Equal(original.LeafNode.Port, roundTripped.LeafNode.Port);
        Assert.Equal(original.LeafNode.RemoteUrls[0], roundTripped.LeafNode.RemoteUrls[0]);
        Assert.Equal(original.LeafNode.ImportSubjects[0], roundTripped.LeafNode.ImportSubjects[0]);
    }

    [Fact]
    public void MapToServerConfig_WithNullAuth_CreatesDefaultAuth()
    {
        // Arrange
        var brokerConfig = new BrokerConfiguration();

        // Act
        var serverConfig = ConfigurationMapper.MapToServerConfig(brokerConfig);

        // Assert
        Assert.NotNull(serverConfig.Auth);
    }

    [Fact]
    public void MapToServerConfig_WithNullLeafNode_CreatesDefaultLeafNode()
    {
        // Arrange
        var brokerConfig = new BrokerConfiguration();

        // Act
        var serverConfig = ConfigurationMapper.MapToServerConfig(brokerConfig);

        // Assert
        Assert.NotNull(serverConfig.LeafNode);
    }
}
