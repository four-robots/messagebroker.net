using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Bindings;
using MessageBroker.Nats.Implementation;
using Moq;
using Xunit;

namespace MessageBroker.Nats.Tests.Implementation;

/// <summary>
/// Unit tests for NatsController monitoring endpoint methods.
/// Tests IntPtr management, error handling, and thread safety without requiring native bindings.
/// </summary>
public class NatsControllerMonitoringTests : IDisposable
{
    private readonly Mock<INatsBindings> _mockBindings;
    private readonly NatsController _controller;

    public NatsControllerMonitoringTests()
    {
        _mockBindings = new Mock<INatsBindings>();
        _controller = new NatsController(_mockBindings.Object);
    }

    public void Dispose()
    {
        _controller?.Dispose();
    }

    #region Helper Methods

    private IntPtr CreateManagedString(string value)
    {
        return Marshal.StringToHGlobalAnsi(value);
    }

    private void SetupSuccessfulConfiguration()
    {
        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4222
        };

        var successJson = "{\"status\": \"success\"}";
        var infoJson = "{\"port\": 4222, \"host\": \"127.0.0.1\"}";

        _mockBindings.Setup(b => b.StartServer(It.IsAny<string>()))
            .Returns(CreateManagedString(successJson));
        _mockBindings.Setup(b => b.GetServerInfo())
            .Returns(CreateManagedString(infoJson));
        _mockBindings.Setup(b => b.FreeString(It.IsAny<IntPtr>()))
            .Returns(IntPtr.Zero);
        _mockBindings.Setup(b => b.SetCurrentPort(It.IsAny<int>()));

        _controller.ConfigureAsync(config).Wait();
    }

    #endregion

    #region GetConnzAsync Tests

    [Fact]
    public async Task GetConnzAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();

        var connzJson = @"{
            ""num_connections"": 5,
            ""total"": 5,
            ""now"": ""2025-11-15T00:00:00Z"",
            ""conns"": []
        }";

        var connzPtr = CreateManagedString(connzJson);
        _mockBindings.Setup(b => b.GetConnz(null))
            .Returns(connzPtr);

        // Act
        var result = await _controller.GetConnzAsync();

        // Assert
        Assert.NotNull(result);
        var doc = JsonDocument.Parse(result);
        Assert.Equal(5, doc.RootElement.GetProperty("num_connections").GetInt32());

        // Verify FreeString was called
        _mockBindings.Verify(b => b.FreeString(connzPtr), Times.Once);
    }

    [Fact]
    public async Task GetConnzAsync_WithFilter_PassesFilterToBinding()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var filter = "test.*";
        var connzJson = @"{""num_connections"": 0, ""conns"": []}";

        _mockBindings.Setup(b => b.GetConnz(filter))
            .Returns(CreateManagedString(connzJson));

        // Act
        await _controller.GetConnzAsync(filter);

        // Assert
        _mockBindings.Verify(b => b.GetConnz(filter), Times.Once);
    }

    [Fact]
    public async Task GetConnzAsync_ErrorResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var errorPtr = CreateManagedString("ERROR: Failed to get connection info");

        _mockBindings.Setup(b => b.GetConnz(null))
            .Returns(errorPtr);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetConnzAsync());

        Assert.Contains("Failed to get connection info", exception.Message);
        _mockBindings.Verify(b => b.FreeString(errorPtr), Times.Once);
    }

    [Fact]
    public async Task GetConnzAsync_ServerNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange - Don't configure server

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetConnzAsync());
    }

    [Fact]
    public async Task GetConnzAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _controller.GetConnzAsync(cancellationToken: cts.Token));
    }

    #endregion

    #region GetSubszAsync Tests

    [Fact]
    public async Task GetSubszAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var subszJson = @"{
            ""num_subscriptions"": 10,
            ""total"": 10
        }";

        _mockBindings.Setup(b => b.GetSubsz(null))
            .Returns(CreateManagedString(subszJson));

        // Act
        var result = await _controller.GetSubszAsync();

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal(10, doc.RootElement.GetProperty("num_subscriptions").GetInt32());
    }

    [Fact]
    public async Task GetSubszAsync_ErrorResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var errorPtr = CreateManagedString("ERROR: Failed to get subscription info");

        _mockBindings.Setup(b => b.GetSubsz(null))
            .Returns(errorPtr);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetSubszAsync());

        Assert.Contains("Failed to get subscription info", exception.Message);
    }

    #endregion

    #region GetJszAsync Tests

    [Fact]
    public async Task GetJszAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var jszJson = @"{
            ""config"": {
                ""max_memory"": 1000000,
                ""max_storage"": 5000000
            },
            ""streams"": 3,
            ""consumers"": 5
        }";

        _mockBindings.Setup(b => b.GetJsz(null))
            .Returns(CreateManagedString(jszJson));

        // Act
        var result = await _controller.GetJszAsync();

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal(3, doc.RootElement.GetProperty("streams").GetInt32());
        Assert.Equal(5, doc.RootElement.GetProperty("consumers").GetInt32());
    }

    [Fact]
    public async Task GetJszAsync_WithAccountFilter_PassesAccountToBinding()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var account = "$G";
        var jszJson = @"{""streams"": 0}";

        _mockBindings.Setup(b => b.GetJsz(account))
            .Returns(CreateManagedString(jszJson));

        // Act
        await _controller.GetJszAsync(account);

        // Assert
        _mockBindings.Verify(b => b.GetJsz(account), Times.Once);
    }

    #endregion

    #region GetRoutezAsync Tests

    [Fact]
    public async Task GetRoutezAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var routezJson = @"{
            ""num_routes"": 2,
            ""routes"": []
        }";

        _mockBindings.Setup(b => b.GetRoutez())
            .Returns(CreateManagedString(routezJson));

        // Act
        var result = await _controller.GetRoutezAsync();

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal(2, doc.RootElement.GetProperty("num_routes").GetInt32());
    }

    #endregion

    #region GetLeafzAsync Tests

    [Fact]
    public async Task GetLeafzAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var leafzJson = @"{
            ""num_leafs"": 1,
            ""leafs"": []
        }";

        _mockBindings.Setup(b => b.GetLeafz())
            .Returns(CreateManagedString(leafzJson));

        // Act
        var result = await _controller.GetLeafzAsync();

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal(1, doc.RootElement.GetProperty("num_leafs").GetInt32());
    }

    #endregion

    #region DisconnectClientAsync Tests

    [Fact]
    public async Task DisconnectClientAsync_Success_ReturnsTrue()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        ulong clientId = 12345;

        _mockBindings.Setup(b => b.DisconnectClientByID(clientId))
            .Returns(CreateManagedString("OK"));

        // Act
        var result = await _controller.DisconnectClientAsync(clientId);

        // Assert
        Assert.True(result);
        _mockBindings.Verify(b => b.DisconnectClientByID(clientId), Times.Once);
    }

    [Fact]
    public async Task DisconnectClientAsync_ClientNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        ulong clientId = 99999;

        _mockBindings.Setup(b => b.DisconnectClientByID(clientId))
            .Returns(CreateManagedString("ERROR: Client with ID 99999 not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.DisconnectClientAsync(clientId));
    }

    [Fact]
    public async Task DisconnectClientAsync_ErrorResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        ulong clientId = 123;

        _mockBindings.Setup(b => b.DisconnectClientByID(clientId))
            .Returns(CreateManagedString("ERROR: Some error occurred"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.DisconnectClientAsync(clientId));

        Assert.Contains("Some error occurred", exception.Message);
    }

    #endregion

    #region GetClientInfoAsync Tests

    [Fact]
    public async Task GetClientInfoAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        ulong clientId = 12345;
        var clientInfoJson = @"{
            ""cid"": 12345,
            ""ip"": ""127.0.0.1"",
            ""port"": 54321,
            ""subs"": 5
        }";

        _mockBindings.Setup(b => b.GetClientInfo(clientId))
            .Returns(CreateManagedString(clientInfoJson));

        // Act
        var result = await _controller.GetClientInfoAsync(clientId);

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal(12345, doc.RootElement.GetProperty("cid").GetInt32());
        Assert.Equal("127.0.0.1", doc.RootElement.GetProperty("ip").GetString());
    }

    [Fact]
    public async Task GetClientInfoAsync_ClientNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        ulong clientId = 99999;

        _mockBindings.Setup(b => b.GetClientInfo(clientId))
            .Returns(CreateManagedString("ERROR: Client with ID 99999 not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetClientInfoAsync(clientId));
    }

    #endregion

    #region GetAccountzAsync Tests

    [Fact]
    public async Task GetAccountzAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var accountzJson = @"{
            ""system_account"": ""$SYS"",
            ""accounts"": []
        }";

        _mockBindings.Setup(b => b.GetAccountz(null))
            .Returns(CreateManagedString(accountzJson));

        // Act
        var result = await _controller.GetAccountzAsync();

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal("$SYS", doc.RootElement.GetProperty("system_account").GetString());
    }

    [Fact]
    public async Task GetAccountzAsync_WithAccountName_PassesAccountToBinding()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var account = "MyAccount";
        var accountzJson = @"{""accounts"": []}";

        _mockBindings.Setup(b => b.GetAccountz(account))
            .Returns(CreateManagedString(accountzJson));

        // Act
        await _controller.GetAccountzAsync(account);

        // Assert
        _mockBindings.Verify(b => b.GetAccountz(account), Times.Once);
    }

    #endregion

    #region GetVarzAsync Tests

    [Fact]
    public async Task GetVarzAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var varzJson = @"{
            ""server_id"": ""NATS123"",
            ""version"": ""2.11.0"",
            ""go"": ""1.22"",
            ""cores"": 8,
            ""mem"": 1000000,
            ""connections"": 10
        }";

        _mockBindings.Setup(b => b.GetVarz())
            .Returns(CreateManagedString(varzJson));

        // Act
        var result = await _controller.GetVarzAsync();

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal("NATS123", doc.RootElement.GetProperty("server_id").GetString());
        Assert.Equal("2.11.0", doc.RootElement.GetProperty("version").GetString());
        Assert.Equal(8, doc.RootElement.GetProperty("cores").GetInt32());
        Assert.Equal(10, doc.RootElement.GetProperty("connections").GetInt32());
    }

    [Fact]
    public async Task GetVarzAsync_ErrorResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        _mockBindings.Setup(b => b.GetVarz())
            .Returns(CreateManagedString("ERROR: Failed to get server variables"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetVarzAsync());

        Assert.Contains("Failed to get server variables", exception.Message);
    }

    #endregion

    #region GetGatewayzAsync Tests

    [Fact]
    public async Task GetGatewayzAsync_Success_ReturnsValidJson()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var gatewayzJson = @"{
            ""server_id"": ""NATS123"",
            ""name"": ""GW1"",
            ""outbound_gateways"": [],
            ""inbound_gateways"": []
        }";

        _mockBindings.Setup(b => b.GetGatewayz(null))
            .Returns(CreateManagedString(gatewayzJson));

        // Act
        var result = await _controller.GetGatewayzAsync();

        // Assert
        var doc = JsonDocument.Parse(result);
        Assert.Equal("NATS123", doc.RootElement.GetProperty("server_id").GetString());
        Assert.Equal("GW1", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetGatewayzAsync_WithGatewayName_PassesNameToBinding()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var gatewayName = "MyGateway";
        var gatewayzJson = @"{""name"": ""MyGateway""}";

        _mockBindings.Setup(b => b.GetGatewayz(gatewayName))
            .Returns(CreateManagedString(gatewayzJson));

        // Act
        await _controller.GetGatewayzAsync(gatewayName);

        // Assert
        _mockBindings.Verify(b => b.GetGatewayz(gatewayName), Times.Once);
    }

    #endregion

    #region Memory Management Tests

    [Fact]
    public async Task MonitoringMethods_AlwaysCallFreeString()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var testPtr = CreateManagedString("{\"test\": \"data\"}");

        _mockBindings.Setup(b => b.GetConnz(null)).Returns(testPtr);
        _mockBindings.Setup(b => b.GetSubsz(null)).Returns(testPtr);
        _mockBindings.Setup(b => b.GetJsz(null)).Returns(testPtr);
        _mockBindings.Setup(b => b.GetRoutez()).Returns(testPtr);
        _mockBindings.Setup(b => b.GetLeafz()).Returns(testPtr);
        _mockBindings.Setup(b => b.GetAccountz(null)).Returns(testPtr);
        _mockBindings.Setup(b => b.GetVarz()).Returns(testPtr);
        _mockBindings.Setup(b => b.GetGatewayz(null)).Returns(testPtr);

        // Act
        await _controller.GetConnzAsync();
        await _controller.GetSubszAsync();
        await _controller.GetJszAsync();
        await _controller.GetRoutezAsync();
        await _controller.GetLeafzAsync();
        await _controller.GetAccountzAsync();
        await _controller.GetVarzAsync();
        await _controller.GetGatewayzAsync();

        // Assert - FreeString should be called 8 times
        _mockBindings.Verify(b => b.FreeString(testPtr), Times.Exactly(8));
    }

    [Fact]
    public async Task MonitoringMethods_ErrorResponse_StillCallsFreeString()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var errorPtr = CreateManagedString("ERROR: Test error");

        _mockBindings.Setup(b => b.GetConnz(null)).Returns(errorPtr);

        // Act
        try
        {
            await _controller.GetConnzAsync();
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert - FreeString should still be called even on error
        _mockBindings.Verify(b => b.FreeString(errorPtr), Times.Once);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task MonitoringMethods_ConcurrentCalls_DoNotInterfere()
    {
        // Arrange
        SetupSuccessfulConfiguration();

        _mockBindings.Setup(b => b.GetConnz(null))
            .Returns(() => CreateManagedString("{\"num_connections\": 1}"));
        _mockBindings.Setup(b => b.GetSubsz(null))
            .Returns(() => CreateManagedString("{\"num_subscriptions\": 1}"));
        _mockBindings.Setup(b => b.GetVarz())
            .Returns(() => CreateManagedString("{\"server_id\": \"test\"}"));

        // Act - Make concurrent calls
        var tasks = new[]
        {
            _controller.GetConnzAsync(),
            _controller.GetSubszAsync(),
            _controller.GetVarzAsync(),
            _controller.GetConnzAsync(),
            _controller.GetSubszAsync()
        };

        var results = await Task.WhenAll(tasks);

        // Assert - All calls should succeed
        Assert.Equal(5, results.Length);
        Assert.All(results, r => Assert.NotNull(r));
    }

    #endregion

    #region SetCurrentPort Tests

    [Fact]
    public async Task MonitoringMethods_CallSetCurrentPort_BeforeOperation()
    {
        // Arrange
        SetupSuccessfulConfiguration();
        var connzJson = "{\"num_connections\": 0}";

        _mockBindings.Setup(b => b.GetConnz(null))
            .Returns(CreateManagedString(connzJson));

        // Act
        await _controller.GetConnzAsync();

        // Assert
        _mockBindings.Verify(b => b.SetCurrentPort(4222), Times.AtLeastOnce);
    }

    #endregion
}
