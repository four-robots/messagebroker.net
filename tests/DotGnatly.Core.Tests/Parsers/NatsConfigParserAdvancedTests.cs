using System;
using System.Linq;
using Xunit;
using DotGnatly.Core.Parsers;
using DotGnatly.Core.Configuration;

namespace DotGnatly.Core.Tests.Parsers;

/// <summary>
/// Tests for advanced NATS configuration parser features including accounts, TLS,
/// authorization, and leaf node remotes.
/// </summary>
public class NatsConfigParserAdvancedTests
{
    #region Accounts Parsing Tests

    [Fact]
    public void Parse_SimpleAccount_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
accounts {
    APP: {
        users: [ {user: app-user, password: secret} ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.Accounts);
        Assert.Equal("APP", config.Accounts[0].Name);
        Assert.Single(config.Accounts[0].Users);
        Assert.Equal("app-user", config.Accounts[0].Users[0].User);
        Assert.Equal("secret", config.Accounts[0].Users[0].Password);
    }

    [Fact]
    public void Parse_AccountWithMultipleUsers_ParsesAllUsers()
    {
        // Arrange
        var configContent = @"
accounts {
    APP: {
        users: [
            {user: user1, password: pass1}
            {user: user2, password: pass2}
            {user: user3, password: pass3}
        ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.Accounts);
        Assert.Equal(3, config.Accounts[0].Users.Count);
        Assert.Equal("user1", config.Accounts[0].Users[0].User);
        Assert.Equal("user2", config.Accounts[0].Users[1].User);
        Assert.Equal("user3", config.Accounts[0].Users[2].User);
    }

    [Fact]
    public void Parse_AccountWithJetStream_ParsesJetStreamFlag()
    {
        // Arrange
        var configContent = @"
accounts {
    APP: {
        jetstream: enabled
        users: [ {user: app-user, password: secret} ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.Accounts);
        Assert.True(config.Accounts[0].Jetstream);
    }

    [Fact]
    public void Parse_AccountWithImports_ParsesImportsCorrectly()
    {
        // Arrange
        var configContent = @"
accounts {
    APP: {
        users: [ {user: app-user, password: secret} ]
        imports: [
            {stream: {account: SYS, subject: orders.>}}
            {service: {account: SYS, subject: api.request}}
        ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.Accounts);
        Assert.Equal(2, config.Accounts[0].Imports.Count);

        var streamImport = config.Accounts[0].Imports[0];
        Assert.Equal("stream", streamImport.Type);
        Assert.Equal("SYS", streamImport.Account);
        Assert.Equal("orders.>", streamImport.Subject);

        var serviceImport = config.Accounts[0].Imports[1];
        Assert.Equal("service", serviceImport.Type);
        Assert.Equal("SYS", serviceImport.Account);
        Assert.Equal("api.request", serviceImport.Subject);
    }

    [Fact]
    public void Parse_AccountWithExports_ParsesExportsCorrectly()
    {
        // Arrange
        var configContent = @"
accounts {
    SYS: {
        users: [ {user: admin, password: admin123} ]
        exports: [
            {stream: orders.>}
            {service: api.request}
        ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.Accounts);
        Assert.Equal(2, config.Accounts[0].Exports.Count);

        var streamExport = config.Accounts[0].Exports[0];
        Assert.Equal("stream", streamExport.Type);
        Assert.Equal("orders.>", streamExport.Subject);

        var serviceExport = config.Accounts[0].Exports[1];
        Assert.Equal("service", serviceExport.Type);
        Assert.Equal("api.request", serviceExport.Subject);
    }

    [Fact]
    public void Parse_AccountWithImportToMapping_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
accounts {
    APP: {
        users: [ {user: app-user, password: secret} ]
        imports: [
            {stream: {account: SYS, subject: orders.>, to: myorders.>}}
        ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.Accounts);
        var import = config.Accounts[0].Imports[0];
        Assert.Equal("orders.>", import.Subject);
        Assert.Equal("myorders.>", import.To);
    }

    [Fact]
    public void Parse_AccountWithMappings_ParsesMappingsCorrectly()
    {
        // Arrange
        var configContent = @"
accounts {
    APP: {
        users: [ {user: app-user, password: secret} ]
        mappings: {
            foo: bar
            orders.>: myorders.>
        }
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.Accounts);
        Assert.Equal(2, config.Accounts[0].Mappings.Count);
        Assert.Equal("bar", config.Accounts[0].Mappings["foo"]);
        Assert.Equal("myorders.>", config.Accounts[0].Mappings["orders.>"]);
    }

    [Fact]
    public void Parse_MultipleAccounts_ParsesAllAccounts()
    {
        // Arrange
        var configContent = @"
accounts {
    SYS: {
        users: [ {user: admin, password: admin123} ]
        exports: [
            {stream: orders.>}
        ]
    }
    APP: {
        jetstream: enabled
        users: [ {user: app-user, password: secret} ]
        imports: [
            {stream: {account: SYS, subject: orders.>}}
        ]
    }
    TENANT: {
        users: [ {user: tenant-user, password: tenant123} ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(3, config.Accounts.Count);
        Assert.Equal("SYS", config.Accounts[0].Name);
        Assert.Equal("APP", config.Accounts[1].Name);
        Assert.Equal("TENANT", config.Accounts[2].Name);

        // Verify SYS has exports
        Assert.Single(config.Accounts[0].Exports);

        // Verify APP has JetStream and imports
        Assert.True(config.Accounts[1].Jetstream);
        Assert.Single(config.Accounts[1].Imports);

        // Verify TENANT has users
        Assert.Single(config.Accounts[2].Users);
    }

    [Fact]
    public void Parse_AccountWithServiceResponseType_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
accounts {
    APP: {
        users: [ {user: app-user, password: secret} ]
        imports: [
            {service: {
                account: SYS
                subject: api.request
                to: myapi.request
            }}
        ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        var import = config.Accounts[0].Imports[0];
        Assert.Equal("service", import.Type);
        Assert.Equal("SYS", import.Account);
        Assert.Equal("api.request", import.Subject);
        Assert.Equal("myapi.request", import.To);
    }

    #endregion

    #region TLS Configuration Tests

    [Fact]
    public void Parse_LeafNodeWithTlsBlock_ParsesTlsCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    tls {
        cert_file: ""/path/to/server-cert.pem""
        key_file: ""/path/to/server-key.pem""
        ca_cert_file: ""/path/to/ca-cert.pem""
        verify: true
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Tls);
        Assert.Equal("/path/to/server-cert.pem", config.LeafNode.Tls.CertFile);
        Assert.Equal("/path/to/server-key.pem", config.LeafNode.Tls.KeyFile);
        Assert.Equal("/path/to/ca-cert.pem", config.LeafNode.Tls.CaCertFile);
        Assert.True(config.LeafNode.Tls.VerifyClientCerts);
    }

    [Fact]
    public void Parse_TlsWithTimeout_ParsesTimeoutCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    tls {
        cert_file: ""/path/to/cert.pem""
        key_file: ""/path/to/key.pem""
        timeout: 5
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Tls);
        Assert.Equal(5, config.LeafNode.Tls.Timeout);
    }

    [Fact]
    public void Parse_TlsWithHandshakeFirst_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    tls {
        cert_file: ""/path/to/cert.pem""
        key_file: ""/path/to/key.pem""
        handshake_first: true
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Tls);
        Assert.True(config.LeafNode.Tls.HandshakeFirst);
    }

    [Fact]
    public void Parse_TlsWithInsecure_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    tls {
        cert_file: ""/path/to/cert.pem""
        key_file: ""/path/to/key.pem""
        insecure: true
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Tls);
        Assert.True(config.LeafNode.Tls.Insecure);
    }

    [Fact]
    public void Parse_TlsWithWindowsCertStore_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    tls {
        cert_store: ""WindowsCurrentUser""
        cert_match_by: ""Subject""
        cert_match: ""CN=nats-server""
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Tls);
        Assert.Equal("WindowsCurrentUser", config.LeafNode.Tls.CertStore);
        Assert.Equal("Subject", config.LeafNode.Tls.CertMatchBy);
        Assert.Equal("CN=nats-server", config.LeafNode.Tls.CertMatch);
    }

    [Fact]
    public void Parse_TlsWithPinnedCerts_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    tls {
        cert_file: ""/path/to/cert.pem""
        key_file: ""/path/to/key.pem""
        pinned_certs: [
            ""cert1-fingerprint""
            ""cert2-fingerprint""
            ""cert3-fingerprint""
        ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Tls);
        Assert.Equal(3, config.LeafNode.Tls.PinnedCerts.Count);
        Assert.Contains("cert1-fingerprint", config.LeafNode.Tls.PinnedCerts);
        Assert.Contains("cert2-fingerprint", config.LeafNode.Tls.PinnedCerts);
        Assert.Contains("cert3-fingerprint", config.LeafNode.Tls.PinnedCerts);
    }

    #endregion

    #region Authorization Block Tests

    [Fact]
    public void Parse_LeafNodeWithAuthorizationBlock_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    authorization {
        user: leafuser
        password: leafpass
        timeout: 5
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Authorization);
        Assert.Equal("leafuser", config.LeafNode.Authorization.User);
        Assert.Equal("leafpass", config.LeafNode.Authorization.Password);
        Assert.Equal(5, config.LeafNode.Authorization.Timeout);
    }

    [Fact]
    public void Parse_AuthorizationWithToken_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    authorization {
        token: ""my-secret-token""
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Authorization);
        Assert.Equal("my-secret-token", config.LeafNode.Authorization.Token);
    }

    [Fact]
    public void Parse_AuthorizationWithAccount_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    authorization {
        account: LEAF_ACCOUNT
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Authorization);
        Assert.Equal("LEAF_ACCOUNT", config.LeafNode.Authorization.Account);
    }

    [Fact]
    public void Parse_AuthorizationWithUsers_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    authorization {
        users: [
            {user: leaf1, password: pass1}
            {user: leaf2, password: pass2}
        ]
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config.LeafNode.Authorization);
        Assert.Equal(2, config.LeafNode.Authorization.Users.Count);
        Assert.Equal("leaf1", config.LeafNode.Authorization.Users[0].User);
        Assert.Equal("leaf2", config.LeafNode.Authorization.Users[1].User);
    }

    #endregion

    #region Leaf Node Remotes Tests

    [Fact]
    public void Parse_LeafNodeWithSimpleRemote_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    remotes: [
        {
            urls: [""nats-leaf://localhost:7422""]
        }
    ]
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.LeafNode.Remotes);
        Assert.Single(config.LeafNode.Remotes[0].Urls);
        Assert.Equal("nats-leaf://localhost:7422", config.LeafNode.Remotes[0].Urls[0]);
    }

    [Fact]
    public void Parse_LeafNodeWithMultipleRemoteUrls_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    remotes: [
        {
            urls: [
                ""nats-leaf://hub1.example.com:7422""
                ""nats-leaf://hub2.example.com:7422""
                ""nats-leaf://hub3.example.com:7422""
            ]
        }
    ]
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.LeafNode.Remotes);
        Assert.Equal(3, config.LeafNode.Remotes[0].Urls.Count);
        Assert.Equal("nats-leaf://hub1.example.com:7422", config.LeafNode.Remotes[0].Urls[0]);
        Assert.Equal("nats-leaf://hub2.example.com:7422", config.LeafNode.Remotes[0].Urls[1]);
        Assert.Equal("nats-leaf://hub3.example.com:7422", config.LeafNode.Remotes[0].Urls[2]);
    }

    [Fact]
    public void Parse_LeafNodeRemoteWithAccount_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    remotes: [
        {
            urls: [""nats-leaf://localhost:7422""]
            account: APP
        }
    ]
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.LeafNode.Remotes);
        Assert.Equal("APP", config.LeafNode.Remotes[0].Account);
    }

    [Fact]
    public void Parse_LeafNodeRemoteWithCredentials_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    remotes: [
        {
            urls: [""nats-leaf://localhost:7422""]
            credentials: ""/path/to/credentials.creds""
        }
    ]
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.LeafNode.Remotes);
        Assert.Equal("/path/to/credentials.creds", config.LeafNode.Remotes[0].Credentials);
    }

    [Fact]
    public void Parse_LeafNodeRemoteWithTls_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    remotes: [
        {
            urls: [""nats-leaf://localhost:7422""]
            tls {
                cert_file: ""/path/to/client-cert.pem""
                key_file: ""/path/to/client-key.pem""
                ca_cert_file: ""/path/to/ca.pem""
            }
        }
    ]
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Single(config.LeafNode.Remotes);
        Assert.NotNull(config.LeafNode.Remotes[0].Tls);
        Assert.Equal("/path/to/client-cert.pem", config.LeafNode.Remotes[0].Tls!.CertFile);
        Assert.Equal("/path/to/client-key.pem", config.LeafNode.Remotes[0].Tls!.KeyFile);
        Assert.Equal("/path/to/ca.pem", config.LeafNode.Remotes[0].Tls!.CaCertFile);
    }

    [Fact]
    public void Parse_LeafNodeWithMultipleRemotes_ParsesAllRemotes()
    {
        // Arrange
        var configContent = @"
leafnodes {
    remotes: [
        {
            urls: [""nats-leaf://hub1.example.com:7422""]
            account: APP
        }
        {
            urls: [""nats-leaf://hub2.example.com:7422""]
            account: SYS
        }
    ]
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(2, config.LeafNode.Remotes.Count);
        Assert.Equal("APP", config.LeafNode.Remotes[0].Account);
        Assert.Equal("SYS", config.LeafNode.Remotes[1].Account);
    }

    #endregion

    #region Leaf Node Properties Tests

    [Fact]
    public void Parse_LeafNodeWithAdvertise_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    advertise: ""10.0.0.21:7422""
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(7422, config.LeafNode.Port);
        Assert.Equal("10.0.0.21:7422", config.LeafNode.Advertise);
    }

    [Fact]
    public void Parse_LeafNodeWithIsolateLeafnodeInterest_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    isolate_leafnode_interest: true
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.True(config.LeafNode.IsolateLeafnodeInterest);
    }

    [Fact]
    public void Parse_LeafNodeWithReconnectDelay_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    reconnect_delay: 2s
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("2s", config.LeafNode.ReconnectDelay);
    }

    #endregion

    #region Complex Integration Tests

    [Fact]
    public void Parse_CompleteLeafNodeConfiguration_ParsesAllProperties()
    {
        // Arrange
        var configContent = @"
leafnodes {
    port: 7422
    host: ""0.0.0.0""
    advertise: ""10.0.0.21:7422""
    isolate_leafnode_interest: true
    reconnect_delay: 2s

    tls {
        cert_file: ""/path/to/server-cert.pem""
        key_file: ""/path/to/server-key.pem""
        ca_cert_file: ""/path/to/ca.pem""
        verify: true
    }

    authorization {
        user: leafuser
        password: leafpass
        timeout: 5
    }

    remotes: [
        {
            urls: [
                ""nats-leaf://hub1.example.com:7422""
                ""nats-leaf://hub2.example.com:7422""
            ]
            account: APP
            tls {
                cert_file: ""/path/to/client-cert.pem""
                key_file: ""/path/to/client-key.pem""
            }
        }
    ]
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(7422, config.LeafNode.Port);
        Assert.Equal("0.0.0.0", config.LeafNode.Host);
        Assert.Equal("10.0.0.21:7422", config.LeafNode.Advertise);
        Assert.True(config.LeafNode.IsolateLeafnodeInterest);
        Assert.Equal("2s", config.LeafNode.ReconnectDelay);

        Assert.NotNull(config.LeafNode.Tls);
        Assert.NotNull(config.LeafNode.Authorization);
        Assert.Single(config.LeafNode.Remotes);
        Assert.Equal(2, config.LeafNode.Remotes[0].Urls.Count);
        Assert.NotNull(config.LeafNode.Remotes[0].Tls);
    }

    [Fact]
    public void Parse_CompleteAccountConfiguration_ParsesAllProperties()
    {
        // Arrange
        var configContent = @"
accounts {
    SYS: {
        users: [
            {user: admin, password: $2a$11$hashedpassword}
        ]
        exports: [
            {stream: orders.>}
            {service: api.request}
        ]
    }
    APP: {
        jetstream: enabled
        users: [
            {user: app-user1, password: secret1}
            {user: app-user2, password: secret2}
        ]
        imports: [
            {stream: {account: SYS, subject: orders.>, to: myorders.>}}
            {service: {account: SYS, subject: api.request}}
        ]
        mappings: {
            foo: bar
            test.>: mapped.>
        }
    }
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(2, config.Accounts.Count);

        // Verify SYS account
        var sysAccount = config.Accounts[0];
        Assert.Equal("SYS", sysAccount.Name);
        Assert.Single(sysAccount.Users);
        Assert.Equal(2, sysAccount.Exports.Count);

        // Verify APP account
        var appAccount = config.Accounts[1];
        Assert.Equal("APP", appAccount.Name);
        Assert.True(appAccount.Jetstream);
        Assert.Equal(2, appAccount.Users.Count);
        Assert.Equal(2, appAccount.Imports.Count);
        Assert.Equal(2, appAccount.Mappings.Count);

        // Verify import with "to" mapping
        var import = appAccount.Imports[0];
        Assert.Equal("orders.>", import.Subject);
        Assert.Equal("myorders.>", import.To);
    }

    #endregion
}
