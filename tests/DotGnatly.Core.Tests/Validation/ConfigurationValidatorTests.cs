using DotGnatly.Core.Configuration;
using DotGnatly.Core.Validation;
using Xunit;

namespace DotGnatly.Core.Tests.Validation;

public class ConfigurationValidatorTests
{
    private readonly ConfigurationValidator _validator;

    public ConfigurationValidatorTests()
    {
        _validator = new ConfigurationValidator();
    }

    [Fact]
    public void Validate_NullConfiguration_ReturnsError()
    {
        // Act
        var result = _validator.Validate(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("cannot be null"));
    }

    [Fact]
    public void Validate_ValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Host = "localhost",
            Port = 4222,
            MaxPayload = 1048576
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(100000)]
    public void Validate_InvalidPort_ReturnsError(int port)
    {
        // Arrange
        var config = new BrokerConfiguration { Port = port };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Port");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4222)]
    [InlineData(8080)]
    [InlineData(65535)]
    public void Validate_ValidPort_ReturnsSuccess(int port)
    {
        // Arrange
        var config = new BrokerConfiguration { Port = port };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyHost_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration { Host = "" };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Host");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_InvalidMaxPayload_ReturnsError(int maxPayload)
    {
        // Arrange
        var config = new BrokerConfiguration { MaxPayload = maxPayload };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MaxPayload");
    }

    [Fact]
    public void Validate_ExcessiveMaxPayload_ReturnsWarning()
    {
        // Arrange
        var config = new BrokerConfiguration { MaxPayload = 20 * 1024 * 1024 }; // 20MB

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid); // Warnings don't make validation invalid
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "MaxPayload" && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_JetStreamEnabled_WithoutStoreDir_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Jetstream = true,
            JetstreamStoreDir = ""
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "JetstreamStoreDir");
    }

    [Fact]
    public void Validate_JetStreamEnabled_WithZeroMemory_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Jetstream = true,
            JetstreamMaxMemory = 0
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "JetstreamMaxMemory");
    }

    [Fact]
    public void Validate_HttpPortSameAsMainPort_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            HttpPort = 4222
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "HttpPort" && e.ErrorMessage.Contains("same as"));
    }

    [Fact]
    public void Validate_HttpsPortSameAsHttpPort_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            HttpPort = 8080,
            HttpsPort = 8080
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "HttpsPort" && e.ErrorMessage.Contains("HttpPort"));
    }

    [Fact]
    public void Validate_TraceWithoutDebug_ReturnsWarning()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Trace = true,
            Debug = false
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid); // Warnings don't make validation invalid
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Trace" && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_UsernameWithoutPassword_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Auth = new AuthConfiguration
            {
                Username = "user",
                Password = null
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Username"));
    }

    [Fact]
    public void Validate_PasswordWithoutUsername_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Auth = new AuthConfiguration
            {
                Username = null,
                Password = "pass"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Password"));
    }

    [Fact]
    public void Validate_UsernamePasswordAndToken_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Auth = new AuthConfiguration
            {
                Username = "user",
                Password = "pass",
                Token = "token"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Auth" && e.ErrorMessage.Contains("simultaneously"));
    }

    [Fact]
    public void Validate_LeafNodePortSameAsMainPort_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 4222
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("LeafNode") && e.ErrorMessage.Contains("same as"));
    }

    [Fact]
    public void Validate_LeafNodeCertWithoutKey_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                TlsCert = "/path/to/cert",
                TlsKey = null
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("TlsCert"));
    }

    [Fact]
    public void Validate_LeafNodeKeyWithoutCert_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                TlsCert = null,
                TlsKey = "/path/to/key"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("TlsKey"));
    }

    [Theory]
    [InlineData("foo.bar")]
    [InlineData("foo.bar.baz")]
    [InlineData("foo.*")]
    [InlineData("foo.>")]
    [InlineData(">")]
    [InlineData("foo-bar.baz_qux")]
    public void Validate_ValidSubjectPattern_Succeeds(string subject)
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                ImportSubjects = new List<string> { subject }
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".foo")]
    [InlineData("foo.")]
    [InlineData("foo..bar")]
    [InlineData("foo.>.bar")]
    [InlineData("foo bar")]
    public void Validate_InvalidSubjectPattern_ReturnsError(string subject)
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                ImportSubjects = new List<string> { subject }
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateChanges_PortChange_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration { Port = 4222 };
        var proposed = new BrokerConfiguration { Port = 5000 };

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Port" && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void ValidateChanges_HostChange_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration { Host = "localhost" };
        var proposed = new BrokerConfiguration { Host = "0.0.0.0" };

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Host" && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void ValidateChanges_EnablingJetStream_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration { Jetstream = false };
        var proposed = new BrokerConfiguration { Jetstream = true };

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Jetstream" && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void ValidateChanges_DisablingJetStream_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration { Jetstream = true };
        var proposed = new BrokerConfiguration { Jetstream = false };

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Jetstream" && e.Severity == ValidationSeverity.Warning && e.ErrorMessage.Contains("loss"));
    }

    [Fact]
    public void ValidateChanges_LargeMaxPayloadChange_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration { MaxPayload = 1000000 };
        var proposed = new BrokerConfiguration { MaxPayload = 100000 }; // 90% reduction

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "MaxPayload" && e.Severity == ValidationSeverity.Warning);
    }

    // Cluster Configuration Tests

    [Fact]
    public void Validate_ClusterEnabled_WithoutName_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = null
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.Name"));
    }

    [Fact]
    public void Validate_ClusterEnabled_WithName_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ClusterPortSameAsMainPort_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            Cluster = new ClusterConfiguration
            {
                Port = 4222,
                Name = "my-cluster"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.Port") && e.ErrorMessage.Contains("main server port"));
    }

    [Fact]
    public void Validate_ClusterPortSameAsHttpPort_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            HttpPort = 8080,
            Cluster = new ClusterConfiguration
            {
                Port = 8080,
                Name = "my-cluster"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.Port") && e.ErrorMessage.Contains("HTTP monitoring port"));
    }

    [Fact]
    public void Validate_ClusterPortSameAsLeafNodePort_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration { Port = 7422 },
            Cluster = new ClusterConfiguration
            {
                Port = 7422,
                Name = "my-cluster"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.Port") && e.ErrorMessage.Contains("leaf node port"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(100000)]
    public void Validate_ClusterInvalidPort_ReturnsError(int port)
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = port,
                Name = "my-cluster"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.Port"));
    }

    [Fact]
    public void Validate_ClusterCertWithoutKey_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                TlsCert = "/path/to/cert",
                TlsKey = null
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.TlsCert"));
    }

    [Fact]
    public void Validate_ClusterKeyWithoutCert_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                TlsCert = null,
                TlsKey = "/path/to/key"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.TlsKey"));
    }

    [Fact]
    public void Validate_ClusterUsernameWithoutPassword_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                AuthUsername = "user",
                AuthPassword = null
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.AuthUsername"));
    }

    [Fact]
    public void Validate_ClusterPasswordWithoutUsername_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                AuthUsername = null,
                AuthPassword = "pass"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.AuthPassword"));
    }

    [Fact]
    public void Validate_ClusterUsernamePasswordAndToken_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                AuthUsername = "user",
                AuthPassword = "pass",
                AuthToken = "token"
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.Auth"));
    }

    [Theory]
    [InlineData("nats-route://localhost:6222")]
    [InlineData("nats://server1:6222")]
    [InlineData("nats-route://192.168.1.1:6222")]
    public void Validate_ClusterValidRouteUrl_Succeeds(string routeUrl)
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                Routes = new List<string> { routeUrl }
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://localhost:6222")]
    [InlineData("invalid-url")]
    [InlineData("nats-route://localhost")]
    public void Validate_ClusterInvalidRouteUrl_ReturnsError(string routeUrl)
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                Routes = new List<string> { routeUrl }
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.Routes"));
    }

    [Fact]
    public void Validate_ClusterWithoutRoutes_ReturnsWarning()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                Routes = new List<string>()
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid); // Warnings don't make validation invalid
        Assert.Contains(result.Errors, e =>
            e.PropertyName.Contains("Cluster.Routes") && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_ClusterNegativeConnectTimeout_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration
            {
                Port = 6222,
                Name = "my-cluster",
                ConnectTimeout = -1
            }
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Cluster.ConnectTimeout"));
    }

    [Fact]
    public void ValidateChanges_EnablingCluster_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration { Port = 0 }
        };
        var proposed = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration { Port = 6222, Name = "my-cluster" }
        };

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        Assert.Contains(result.Errors, e =>
            e.PropertyName.Contains("Cluster.Port") && e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void ValidateChanges_DisablingCluster_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration { Port = 6222, Name = "my-cluster" }
        };
        var proposed = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration { Port = 0 }
        };

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        Assert.Contains(result.Errors, e =>
            e.PropertyName.Contains("Cluster.Port") &&
            e.Severity == ValidationSeverity.Warning &&
            e.ErrorMessage.Contains("disconnect"));
    }

    [Fact]
    public void ValidateChanges_ChangingClusterName_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration { Port = 6222, Name = "cluster-1" }
        };
        var proposed = new BrokerConfiguration
        {
            Cluster = new ClusterConfiguration { Port = 6222, Name = "cluster-2" }
        };

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        Assert.Contains(result.Errors, e =>
            e.PropertyName.Contains("Cluster.Name") &&
            e.Severity == ValidationSeverity.Warning &&
            e.ErrorMessage.Contains("restart"));
    }

    // Logging Configuration Tests

    [Fact]
    public void Validate_LogFileSize_Negative_ReturnsError()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LogFileSize = -1
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LogFileSize");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    [InlineData(1048576)]
    [InlineData(10485760)]
    public void Validate_LogFileSize_ValidValues_Succeeds(long logFileSize)
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LogFileSize = logFileSize
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LogFile_ValidPath_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LogFile = "/var/log/nats.log"
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LogFile_NullValue_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LogFile = null
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LogTimeUtc_True_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LogTimeUtc = true
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_LogTimeUtc_False_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            LogTimeUtc = false
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    // JetStream Clustering Tests

    [Fact]
    public void Validate_JetstreamDomain_ValidValue_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Jetstream = true,
            JetstreamDomain = "production"
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_JetstreamUniqueTag_ValidValue_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Jetstream = true,
            JetstreamUniqueTag = "server-001"
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_JetstreamDomain_WithoutJetstreamEnabled_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Jetstream = false,
            JetstreamDomain = "production"
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        // This should succeed - domain is ignored if JetStream is disabled
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_JetstreamUniqueTag_WithoutJetstreamEnabled_Succeeds()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Jetstream = false,
            JetstreamUniqueTag = "server-001"
        };

        // Act
        var result = _validator.Validate(config);

        // Assert
        // This should succeed - unique tag is ignored if JetStream is disabled
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateChanges_ChangingJetstreamDomain_ReturnsWarning()
    {
        // Arrange
        var current = new BrokerConfiguration
        {
            Jetstream = true,
            JetstreamDomain = "domain-1"
        };
        var proposed = new BrokerConfiguration
        {
            Jetstream = true,
            JetstreamDomain = "domain-2"
        };

        // Act
        var result = _validator.ValidateChanges(current, proposed);

        // Assert
        // Changing JetStream domain might require careful consideration
        Assert.Contains(result.Errors, e =>
            e.PropertyName.Contains("JetstreamDomain") ||
            e.Severity == ValidationSeverity.Warning);
    }
}
