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

    [Theory]
    [InlineData("1KB", 1024)]
    [InlineData("1K", 1024)]
    [InlineData("5MB", 5 * 1024 * 1024)]
    [InlineData("5M", 5 * 1024 * 1024)]
    [InlineData("2GB", 2L * 1024 * 1024 * 1024)]
    [InlineData("2G", 2L * 1024 * 1024 * 1024)]
    [InlineData("1024", 1024)]
    [InlineData("0", 0)]
    public void ParseSize_VariousFormats_ReturnsCorrectBytes(string input, long expected)
    {
        // Act
        var result = NatsConfigParser.ParseSize(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1s", 1)]
    [InlineData("60s", 60)]
    [InlineData("1m", 60)]
    [InlineData("5m", 300)]
    [InlineData("1h", 3600)]
    [InlineData("2h", 7200)]
    [InlineData("30", 30)]
    public void ParseTimeSeconds_VariousFormats_ReturnsCorrectSeconds(string input, int expected)
    {
        // Act
        var result = NatsConfigParser.ParseTimeSeconds(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsDefaultConfiguration()
    {
        // Arrange
        var configContent = "";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("localhost", config.Host);
        Assert.Equal(4222, config.Port);
    }

    [Fact]
    public void Parse_OnlyComments_ReturnsDefaultConfiguration()
    {
        // Arrange
        var configContent = @"
# This is a comment
# Another comment
# Yet another comment
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("localhost", config.Host);
        Assert.Equal(4222, config.Port);
    }

    [Fact]
    public void Parse_WithEqualsSign_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen = 127.0.0.1:4222
server_name = ""test-server""
monitor_port = 8222
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test-server", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.Equal(4222, config.Port);
    }

    [Fact]
    public void Parse_MixedColonAndEquals_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
server_name = ""test-server""
monitor_port: 8222
max_payload = 8MB
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test-server", config.ServerName);
        Assert.Equal(8222, config.HttpPort);
        Assert.Equal(8 * 1024 * 1024, config.MaxPayload);
    }

    [Theory]
    [InlineData("enabled")]
    [InlineData("true")]
    [InlineData("yes")]
    [InlineData("1")]
    public void Parse_JetStreamEnabled_VariousFormats(string value)
    {
        // Arrange
        var configContent = $@"
listen: 127.0.0.1:4222
jetstream: {value}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.True(config.Jetstream);
    }

    [Fact]
    public void Parse_JetStreamWithAllProperties_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
jetstream: enabled
jetstream {
    store_dir=""/var/lib/nats/data""
    domain=""test-domain""
    max_memory=1GB
    max_file=10GB
    unique_tag=""server-01""
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.True(config.Jetstream);
        Assert.Equal("/var/lib/nats/data", config.JetstreamStoreDir);
        Assert.Equal("test-domain", config.JetstreamDomain);
        Assert.Equal(1L * 1024 * 1024 * 1024, config.JetstreamMaxMemory);
        Assert.Equal(10L * 1024 * 1024 * 1024, config.JetstreamMaxStore);
        Assert.Equal("server-01", config.JetstreamUniqueTag);
    }

    [Fact]
    public void Parse_LeafNodeWithHost_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
leafnodes {
    host: 0.0.0.0
    port: 7422
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("0.0.0.0", config.LeafNode.Host);
        Assert.Equal(7422, config.LeafNode.Port);
    }

    [Fact]
    public void Parse_QuotedStrings_RemovesQuotes()
    {
        // Arrange
        var configContent = @"
server_name: ""test-server""
log_file: '/var/log/nats.log'
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test-server", config.ServerName);
        Assert.Equal("/var/log/nats.log", config.LogFile);
    }

    [Fact]
    public void Parse_UnquotedStrings_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
server_name: test-server
system_account: SYS
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("test-server", config.ServerName);
        Assert.Equal("SYS", config.SystemAccount);
    }

    [Fact]
    public void Parse_MultipleWriteDeadlineFormats_ParsesCorrectly()
    {
        // Arrange
        var configContent = @"
listen: 127.0.0.1:4222
write_deadline: 30s
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal(30, config.WriteDeadline);
    }

    [Fact]
    public void ParseFile_NonExistentFile_ThrowsException()
    {
        // Arrange
        var filePath = "/tmp/non-existent-file-12345.conf";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => NatsConfigParser.ParseFile(filePath));
    }

    [Fact]
    public void Parse_CompleteConfiguration_ParsesAllProperties()
    {
        // Arrange
        var configContent = @"
listen: 192.168.1.100:4223
server_name: ""production-server""
monitor_port: 8223
debug: true
trace: false
log_file: ""/var/log/nats/production.log""
logfile_size_limit: 500MB
logfile_max_num: 20
max_payload: 16MB
write_deadline: 15s
disable_sublist_cache: true
system_account: SYSTEM
jetstream: enabled
jetstream {
    store_dir=""/data/nats/jetstream""
    domain=""production""
    max_memory=8GB
    max_file=100GB
    unique_tag=""prod-01""
}
leafnodes {
    host: 10.0.0.1
    port: 7422
}
";

        // Act
        var config = NatsConfigParser.Parse(configContent);

        // Assert
        Assert.Equal("192.168.1.100", config.Host);
        Assert.Equal(4223, config.Port);
        Assert.Equal("production-server", config.ServerName);
        Assert.Equal(8223, config.HttpPort);
        Assert.True(config.Debug);
        Assert.False(config.Trace);
        Assert.Equal("/var/log/nats/production.log", config.LogFile);
        Assert.Equal(500L * 1024 * 1024, config.LogFileSize);
        Assert.Equal(20, config.LogFileMaxNum);
        Assert.Equal(16 * 1024 * 1024, config.MaxPayload);
        Assert.Equal(15, config.WriteDeadline);
        Assert.True(config.DisableSublistCache);
        Assert.Equal("SYSTEM", config.SystemAccount);
        Assert.True(config.Jetstream);
        Assert.Equal("/data/nats/jetstream", config.JetstreamStoreDir);
        Assert.Equal("production", config.JetstreamDomain);
        Assert.Equal(8L * 1024 * 1024 * 1024, config.JetstreamMaxMemory);
        Assert.Equal(100L * 1024 * 1024 * 1024, config.JetstreamMaxStore);
        Assert.Equal("prod-01", config.JetstreamUniqueTag);
        Assert.Equal("10.0.0.1", config.LeafNode.Host);
        Assert.Equal(7422, config.LeafNode.Port);
    }
}
