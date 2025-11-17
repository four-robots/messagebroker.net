using Xunit;
using DotGnatly.Core.Parsers;
using DotGnatly.Core.Configuration;

namespace DotGnatly.Core.Tests.Parsers;

public class NatsConfigParserTests
{
    [Fact]
    public void ParseSize_WithMegabytes_ReturnsCorrectBytes()
    {
        // Arrange
        var input = "8MB";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(8 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseSize_WithLowercaseMb_ReturnsCorrectBytes()
    {
        // Arrange
        var input = "100Mb";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(100 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseSize_WithGigabytes_ReturnsCorrectBytes()
    {
        // Arrange
        var input = "2GB";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(2L * 1024 * 1024 * 1024, result);
    }

    [Fact]
    public void ParseSize_WithKilobytes_ReturnsCorrectBytes()
    {
        // Arrange
        var input = "512KB";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(512 * 1024, result);
    }

    [Fact]
    public void ParseSize_WithPlainNumber_ReturnsNumber()
    {
        // Arrange
        var input = "1048576";

        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(1048576, result);
    }

    [Fact]
    public void ParseTimeSeconds_WithSeconds_ReturnsCorrectValue()
    {
        // Arrange
        var input = "10s";

        // Act
        var result = NatsConfigParser.ParseTimeSeconds(input);

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void ParseTimeSeconds_WithMinutes_ReturnsCorrectSeconds()
    {
        // Arrange
        var input = "2m";

        // Act
        var result = NatsConfigParser.ParseTimeSeconds(input);

        // Assert
        Assert.Equal(120, result);
    }

    [Fact]
    public void ParseTimeSeconds_WithHours_ReturnsCorrectSeconds()
    {
        // Arrange
        var input = "1h";

        // Act
        var result = NatsConfigParser.ParseTimeSeconds(input);

        // Assert
        Assert.Equal(3600, result);
    }

    [Fact]
    public void Parse_SimpleInstallConfig_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
server_name: ""nats-server-01""
monitor_port: 8222
jetstream: enabled
jetstream {
    store_dir=""/var/lib/nats/data-store""
    domain=basic
}
log_file: ""/var/log/nats/nats.log""
max_payload: 8MB
system_account: SYS
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Equal(4222, config.Port);
        Assert.Equal("nats-server-01", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.True(config.Jetstream);
        Assert.Equal("/var/lib/nats/data-store", config.JetstreamStoreDir);
        Assert.Equal("basic", config.JetstreamDomain);
        Assert.Equal("/var/log/nats/nats.log", config.LogFile);
        Assert.Equal(8 * 1024 * 1024, config.MaxPayload);
        Assert.Equal("SYS", config.SystemAccount);
    }

    [Fact]
    public void Parse_LeafConfigWithLogSettings_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4223
server_name: ""nats-leaf-01""
monitor_port: 8223
debug: false
trace: false
log_file: ""/var/log/nats/nats-leaf-01.log""
logfile_size_limit: 100Mb
logfile_max_num: 10
max_payload: 8MB
write_deadline: 10s
disable_sublist_cache: false
jetstream: enabled
jetstream {
    store_dir = ""/var/lib/nats/leaf-data""
    domain = ""leaf-01""
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("nats-leaf-01", config.ServerName);
        Assert.Equal(4223, config.Port);
        Assert.Equal(8223, config.HttpPort);
        Assert.False(config.Debug);
        Assert.False(config.Trace);
        Assert.Equal("/var/log/nats/nats-leaf-01.log", config.LogFile);
        Assert.Equal(100 * 1024 * 1024, config.LogFileSize);
        Assert.Equal(10, config.LogFileMaxNum);
        Assert.Equal(8 * 1024 * 1024, config.MaxPayload);
        Assert.Equal(10, config.WriteDeadline);
        Assert.False(config.DisableSublistCache);
        Assert.True(config.Jetstream);
        Assert.Equal("/var/lib/nats/leaf-data", config.JetstreamStoreDir);
        Assert.Equal("leaf-01", config.JetstreamDomain);
    }

    [Fact]
    public void Parse_HubConfigWithLeafNodes_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
server_name: ""nats-hub""
monitor_port: 8222
jetstream: enabled
jetstream {
    store_dir=""/var/lib/nats/hub-data""
    domain=hub
}
leafnodes {
    port: 7422
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("nats-hub", config.ServerName);
        Assert.True(config.Jetstream);
        Assert.Equal("/var/lib/nats/hub-data", config.JetstreamStoreDir);
        Assert.Equal("hub", config.JetstreamDomain);
        Assert.Equal(7422, config.LeafNode.Port);
    }

    [Fact]
    public void Parse_WithComments_IgnoresComments()
    {
        // Arrange
        var configContent = @"
# This is a comment
listen: 127.0.0.1:4222  # Another comment
server_name: ""test-server""
# monitor_port: 9999
monitor_port: 8222  # The real one
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test-server", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.Equal(4222, config.Port);
    }

    [Fact]
    public void Parse_BooleanValues_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
debug: true
trace: false
disable_sublist_cache: true
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.True(config.Debug);
        Assert.False(config.Trace);
        Assert.True(config.DisableSublistCache);
    }
}
