using System;
using Xunit;
using DotGnatly.Core.Configuration;
using DotGnatly.Core.Validation;

namespace DotGnatly.Core.Tests.Validation;

/// <summary>
/// Tests for TLS configuration validation, including Windows Certificate Store validation.
/// </summary>
public class TlsConfigurationValidationTests
{
    [Fact]
    public void Validate_WindowsCertStoreOnLinux_FailsValidation()
    {
        // Skip this test on Windows since we want to test Linux behavior
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertStore = "WindowsLocalMachine",
                    CertMatchBy = "Subject",
                    CertMatch = "CN=nats-server"
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls" &&
            e.ErrorMessage.Contains("Windows Certificate Store") &&
            e.ErrorMessage.Contains("only supported on Windows"));
    }

    [Fact]
    public void Validate_WindowsCertStoreOnWindows_PassesValidation()
    {
        // Skip this test on Linux since we want to test Windows behavior
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertStore = "WindowsLocalMachine",
                    CertMatchBy = "Subject",
                    CertMatch = "CN=nats-server"
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert - Should not have Windows cert store OS error
        Assert.DoesNotContain(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls" &&
            e.ErrorMessage.Contains("only supported on Windows"));
    }

    [Fact]
    public void Validate_BothCertFilesAndCertStore_GivesWarning()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertFile = "/path/to/cert.pem",
                    KeyFile = "/path/to/key.pem",
                    CertStore = "WindowsLocalMachine",
                    CertMatchBy = "Subject",
                    CertMatch = "CN=nats-server"
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert - Should have warning about using both methods
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls" &&
            e.ErrorMessage.Contains("Cannot use both certificate files") &&
            e.ErrorMessage.Contains("Windows Certificate Store") &&
            e.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public void Validate_CertFileWithoutKey_FailsValidation()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertFile = "/path/to/cert.pem"
                    // KeyFile missing
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls.CertFile" &&
            e.ErrorMessage.Contains("key file is not"));
    }

    [Fact]
    public void Validate_KeyFileWithoutCert_FailsValidation()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    KeyFile = "/path/to/key.pem"
                    // CertFile missing
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls.KeyFile" &&
            e.ErrorMessage.Contains("certificate file is not"));
    }

    [Fact]
    public void Validate_IncompleteCertStore_MissingCertStore_FailsValidation()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    // CertStore missing
                    CertMatchBy = "Subject",
                    CertMatch = "CN=nats-server"
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls.CertStore" &&
            e.ErrorMessage.Contains("cert_store must be specified"));
    }

    [Fact]
    public void Validate_IncompleteCertStore_MissingCertMatch_FailsValidation()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertStore = "WindowsLocalMachine",
                    CertMatchBy = "Subject"
                    // CertMatch missing
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls.CertMatch" &&
            e.ErrorMessage.Contains("cert_match must be specified"));
    }

    [Fact]
    public void Validate_IncompleteCertStore_MissingCertMatchBy_FailsValidation()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertStore = "WindowsLocalMachine",
                    // CertMatchBy missing
                    CertMatch = "CN=nats-server"
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls.CertMatchBy" &&
            e.ErrorMessage.Contains("cert_match_by must be specified"));
    }

    [Fact]
    public void Validate_NegativeTlsTimeout_FailsValidation()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertFile = "/path/to/cert.pem",
                    KeyFile = "/path/to/key.pem",
                    Timeout = -5
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Tls.Timeout" &&
            e.ErrorMessage.Contains("cannot be negative"));
    }

    [Fact]
    public void Validate_RemoteTlsWithWindowsCertStoreOnLinux_FailsValidation()
    {
        // Skip this test on Windows since we want to test Linux behavior
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Remotes = new System.Collections.Generic.List<LeafNodeRemoteConfiguration>
                {
                    new LeafNodeRemoteConfiguration
                    {
                        Urls = new System.Collections.Generic.List<string> { "nats-leaf://hub.example.com:7422" },
                        Tls = new TlsConfiguration
                        {
                            CertStore = "WindowsCurrentUser",
                            CertMatchBy = "Subject",
                            CertMatch = "CN=client-cert"
                        }
                    }
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "LeafNode.Remotes[0].Tls" &&
            e.ErrorMessage.Contains("Windows Certificate Store") &&
            e.ErrorMessage.Contains("only supported on Windows"));
    }

    [Fact]
    public void Validate_ValidCertFileConfiguration_PassesValidation()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertFile = "/path/to/cert.pem",
                    KeyFile = "/path/to/key.pem",
                    CaCertFile = "/path/to/ca.pem",
                    VerifyClientCerts = true,
                    Timeout = 60
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert - Should not have TLS-related errors
        Assert.DoesNotContain(result.Errors, e => e.PropertyName.Contains("Tls"));
    }

    [Fact]
    public void Validate_TlsWithPinnedCerts_PassesValidation()
    {
        // Arrange
        var config = new BrokerConfiguration
        {
            Port = 4222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 7422,
                Tls = new TlsConfiguration
                {
                    CertFile = "/path/to/cert.pem",
                    KeyFile = "/path/to/key.pem",
                    PinnedCerts = new System.Collections.Generic.List<string>
                    {
                        "a2818b4eb95e9e8c7cf26469ed8d1ff0db45737cc54c32932346581cae40ca1c",
                        "6d0dc0ed85d9c58efb33839bf8735290a0eb28faf365ba0893418e84f2ce73a2"
                    }
                }
            }
        };

        var validator = new ConfigurationValidator();

        // Act
        var result = validator.Validate(config);

        // Assert - Should not have TLS-related errors
        Assert.DoesNotContain(result.Errors, e => e.PropertyName.Contains("Tls"));
    }
}
