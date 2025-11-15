using MessageBroker.Core.Configuration;
using MessageBroker.Core.Validation;
using Xunit;

namespace MessageBroker.Core.Tests.Validation;

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
        Assert.Contains(result.Errors, e => e.Message.Contains("cannot be null"));
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
        Assert.False(result.IsValid);
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
        Assert.Contains(result.Errors, e => e.PropertyName == "HttpPort" && e.Message.Contains("same as"));
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
        Assert.Contains(result.Errors, e => e.PropertyName == "HttpsPort" && e.Message.Contains("HttpPort"));
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
        Assert.False(result.IsValid);
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
        Assert.Contains(result.Errors, e => e.PropertyName == "Auth" && e.Message.Contains("simultaneously"));
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
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("LeafNode") && e.Message.Contains("same as"));
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
            e.PropertyName == "Jetstream" && e.Severity == ValidationSeverity.Warning && e.Message.Contains("loss"));
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
}
