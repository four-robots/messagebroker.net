using System;
using System.IO;
using Xunit;
using DotGnatly.Core.Parsers;

namespace DotGnatly.Core.Tests.Parsers;

/// <summary>
/// Tests for error handling and edge cases in NATS configuration parser.
/// </summary>
public class NatsConfigParserErrorTests
{
    [Fact]
    public void ParseSize_InvalidFormat_ReturnsZero()
    {
        // Arrange
        var input = "invalid";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseSize_NegativeValue_ParsesCorrectly()
    {
        // Arrange
        var input = "-1";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void ParseSize_EmptyString_ReturnsZero()
    {
        // Arrange
        var input = "";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseSize_Whitespace_ReturnsZero()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseTimeSeconds_InvalidFormat_ReturnsZero()
    {
        // Arrange
        var input = "invalid";

        // Act
        var result = NatsConfigParser.ParseTimeSeconds(input);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseTimeSeconds_EmptyString_ReturnsZero()
    {
        // Arrange
        var input = "";

        // Act
        var result = NatsConfigParser.ParseTimeSeconds(input);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Parse_NullString_ThrowsArgumentNullException()
    {
        // Arrange
        string? content = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => NatsConfigParser.Parse(content!));
    }

    [Fact]
    public void Parse_MalformedJetStreamBlock_DoesNotThrow()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
jetstream: enabled
jetstream {
    # Missing closing brace
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert - should not throw, just parse what it can
        Assert.NotNull(config);
        Assert.True(config.Jetstream);
    }

    [Fact]
    public void Parse_UnknownProperties_IgnoresGracefully()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
server_name: ""test""
unknown_property: some_value
another_unknown: 12345
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert - should parse known properties and ignore unknown ones
        Assert.NotNull(config);
        Assert.Equal("test", config.ServerName);
        Assert.Equal(4222, config.Port);
    }

    [Fact]
    public void Parse_DuplicateProperties_UsesLastValue()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
server_name: ""first-name""
server_name: ""second-name""
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("second-name", config.ServerName);
    }

    [Fact]
    public void Parse_InvalidListenAddress_HandlesGracefully()
    {
        // Arrange
        var configContent = @"
listen: invalid-address
server_name: ""test""
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert - should use defaults for invalid values
        Assert.NotNull(config);
        Assert.Equal("test", config.ServerName);
    }

    [Fact]
    public void Parse_InvalidPortNumber_HandlesGracefully()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:not-a-number
server_name: ""test""
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("test", config.ServerName);
    }

    [Fact]
    public void Parse_VeryLargePayloadSize_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
max_payload: 100GB
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(100L * 1024 * 1024 * 1024, config.MaxPayload);
    }

    [Fact]
    public void Parse_VeryLongTimeout_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
write_deadline: 24h
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(24 * 3600, config.WriteDeadline);
    }

    [Fact]
    public void Parse_SpecialCharactersInStrings_HandlesCorrectly()
    {
        // Arrange
        var configContent = @"
server_name: ""test-server_01.production""
log_file: ""/var/log/nats/server-01.log""
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test-server_01.production", config.ServerName);
        Assert.Equal("/var/log/nats/server-01.log", config.LogFile);
    }

    [Fact]
    public void Parse_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var configContent = @"
server_name: ""test-服务器-01""
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test-服务器-01", config.ServerName);
    }

    [Fact]
    public void Parse_PathsWithSpaces_HandlesCorrectly()
    {
        // Arrange
        var configContent = @"
log_file: ""/var/log/my nats server/server.log""
jetstream {
    store_dir=""/data/jet stream/store""
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("/var/log/my nats server/server.log", config.LogFile);
        Assert.Equal("/data/jet stream/store", config.JetstreamStoreDir);
    }

    [Fact]
    public void Parse_WindowsPathsWithBackslashes_HandlesCorrectly()
    {
        // Arrange
        var configContent = @"
log_file: ""C:\ProgramData\NATS\logs\server.log""
jetstream {
    store_dir=""C:\ProgramData\NATS\data""
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(@"C:\ProgramData\NATS\logs\server.log", config.LogFile);
        Assert.Equal(@"C:\ProgramData\NATS\data", config.JetstreamStoreDir);
    }

    [Fact]
    public void Parse_MixedIndentation_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
	server_name: ""test""
        monitor_port: 8222
jetstream {
	store_dir=""/tmp/js""
        domain=test
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.Equal("/tmp/js", config.JetstreamStoreDir);
        Assert.Equal("test", config.JetstreamDomain);
    }

    [Fact]
    public void Parse_NestedBlocksWithSameName_HandlesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
jetstream: enabled
jetstream {
    store_dir=""/tmp/js1""
}
# This might override or be ignored
jetstream {
    domain=test
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.True(config.Jetstream);
        // Should have at least one of the properties
        Assert.True(!string.IsNullOrEmpty(config.JetstreamStoreDir) || !string.IsNullOrEmpty(config.JetstreamDomain));
    }

    [Fact]
    public void Parse_ExtraWhitespace_TrimsCorrectly()
    {
        // Arrange
        var configContent = @"
listen:    127.0.0.1:4222
server_name:   ""test""
monitor_port:   8222
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.Equal(4222, config.Port);
    }

    [Fact]
    public void Parse_CaseSensitiveKeys_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
SERVER_NAME: ""should-not-parse""
server_name: ""correct-name""
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        // Should only parse lowercase version
        Assert.Equal("correct-name", config.ServerName);
    }

    [Fact]
    public void ParseSize_DecimalValues_HandlesCorrectly()
    {
        // Arrange
        var input = "1.5MB";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal((long)(1.5 * 1024 * 1024), result);
    }

    [Fact]
    public void ParseTimeSeconds_DecimalValues_HandlesCorrectly()
    {
        // Arrange
        var input = "2.5m";

        // Act
        var result = NatsConfigParser.ParseTimeSeconds(input);

        // Assert
        Assert.Equal(150, result); // 2.5 minutes = 150 seconds
    }
}
