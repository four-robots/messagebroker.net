
using System.Runtime.InteropServices;

namespace MessageBroker.Nats.Bindings;

internal interface INatsBindings
{
    IntPtr StartServer(string configJson);
    IntPtr StartServerWithJetStream(string host, int port, string storeDir);
    IntPtr StartServerFromConfigFile(string configFilePath);
    void ShutdownServer();
    IntPtr EnterLameDuckMode();
    void SetCurrentPort(int port);
    IntPtr GetClientURL();
    IntPtr GetServerInfo();
    IntPtr FreeString(IntPtr ptr);
    IntPtr ReloadConfig();
    IntPtr ReloadConfigFromFile(string configFilePath);
    IntPtr UpdateAndReloadConfig(string configJson);
    IntPtr CreateAccount(string accountJson);
    IntPtr CreateAccountWithJWT(string operatorSeed, string accountConfig);

    // Monitoring endpoints
    IntPtr GetConnz(string? subsFilter);
    IntPtr GetSubsz(string? subsFilter);
    IntPtr GetJsz(string? accountName);
    IntPtr GetRoutez();
    IntPtr GetLeafz();

    // Connection management
    IntPtr DisconnectClientByID(ulong clientId);
    IntPtr GetClientInfo(ulong clientId);

    // Additional monitoring endpoints
    IntPtr GetAccountz(string? accountName);
    IntPtr GetVarz();
    IntPtr GetGatewayz(string? gatewayName);

    // Account management
    IntPtr RegisterAccount(string accountName);
    IntPtr LookupAccount(string accountName);
    IntPtr GetAccountStatz(string? accountFilter);

    // Server state methods
    IntPtr GetServerID();
    IntPtr GetServerName();
    IntPtr IsServerRunning();
    IntPtr WaitForReadyState(int timeoutSeconds);
    IntPtr IsJetStreamEnabled();
}

internal sealed class WindowsNatsBindings : INatsBindings
{
    // Basic server DllImport declarations
    [DllImport("nats-bindings.dll", EntryPoint = "StartServer")]
    internal static extern IntPtr _startServer(string configJson);

    public IntPtr StartServer(string configJson) => _startServer(configJson);

    [DllImport("nats-bindings.dll", EntryPoint = "StartServerWithJetStream")]
    internal static extern IntPtr _startServerWithJetStream(string host, int port, string storeDir);

    public IntPtr StartServerWithJetStream(string host, int port, string storeDir) => _startServerWithJetStream(host, port, storeDir);

    [DllImport("nats-bindings.dll", EntryPoint = "StartServerFromConfigFile")]
    internal static extern IntPtr _startServerFromConfigFile(string configFilePath);

    public IntPtr StartServerFromConfigFile(string configFilePath) => _startServerFromConfigFile(configFilePath);

    [DllImport("nats-bindings.dll", EntryPoint = "ShutdownServer")]
    internal static extern void _shutdownServer();

    public void ShutdownServer() => _shutdownServer();

    [DllImport("nats-bindings.dll", EntryPoint = "EnterLameDuckMode")]
    internal static extern IntPtr _enterLameDuckMode();

    public IntPtr EnterLameDuckMode() => _enterLameDuckMode();

    [DllImport("nats-bindings.dll", EntryPoint = "SetCurrentPort")]
    internal static extern void _setCurrentPort(int port);

    public void SetCurrentPort(int port) => _setCurrentPort(port);

    [DllImport("nats-bindings.dll", EntryPoint = "GetClientURL")]
    internal static extern IntPtr _getClientURL();

    public IntPtr GetClientURL() => _getClientURL();

    [DllImport("nats-bindings.dll", EntryPoint = "GetServerInfo")]
    internal static extern IntPtr _getServerInfo();

    public IntPtr GetServerInfo() => _getServerInfo();

    [DllImport("nats-bindings.dll", EntryPoint = "FreeString")]
    internal static extern IntPtr _freeString(IntPtr ptr);

    public IntPtr FreeString(IntPtr ptr) => _freeString(ptr);

    // Hot reload function declarations
    [DllImport("nats-bindings.dll", EntryPoint = "ReloadConfig")]
    internal static extern IntPtr _reloadConfig();

    public IntPtr ReloadConfig() => _reloadConfig();

    [DllImport("nats-bindings.dll", EntryPoint = "ReloadConfigFromFile")]
    internal static extern IntPtr _reloadConfigFromFile(string configFilePath);

    public IntPtr ReloadConfigFromFile(string configFilePath) => _reloadConfigFromFile(configFilePath);

    [DllImport("nats-bindings.dll", EntryPoint = "UpdateAndReloadConfig")]
    internal static extern IntPtr _updateAndReloadConfig(string configJson);

    public IntPtr UpdateAndReloadConfig(string configJson) => _updateAndReloadConfig(configJson);

    // Account management declarations
    [DllImport("nats-bindings.dll", EntryPoint = "CreateAccount")]
    internal static extern IntPtr _createAccount(string accountJson);

    public IntPtr CreateAccount(string accountJson) => _createAccount(accountJson);

    [DllImport("nats-bindings.dll", EntryPoint = "CreateAccountWithJWT")]
    internal static extern IntPtr _createAccountWithJWT(string operatorSeed, string accountConfig);

    public IntPtr CreateAccountWithJWT(string operatorSeed, string accountConfig) => _createAccountWithJWT(operatorSeed, accountConfig);

    // Monitoring endpoints
    [DllImport("nats-bindings.dll", EntryPoint = "GetConnz")]
    internal static extern IntPtr _getConnz(string? subsFilter);

    public IntPtr GetConnz(string? subsFilter) => _getConnz(subsFilter);

    [DllImport("nats-bindings.dll", EntryPoint = "GetSubsz")]
    internal static extern IntPtr _getSubsz(string? subsFilter);

    public IntPtr GetSubsz(string? subsFilter) => _getSubsz(subsFilter);

    [DllImport("nats-bindings.dll", EntryPoint = "GetJsz")]
    internal static extern IntPtr _getJsz(string? accountName);

    public IntPtr GetJsz(string? accountName) => _getJsz(accountName);

    [DllImport("nats-bindings.dll", EntryPoint = "GetRoutez")]
    internal static extern IntPtr _getRoutez();

    public IntPtr GetRoutez() => _getRoutez();

    [DllImport("nats-bindings.dll", EntryPoint = "GetLeafz")]
    internal static extern IntPtr _getLeafz();

    public IntPtr GetLeafz() => _getLeafz();

    // Connection management
    [DllImport("nats-bindings.dll", EntryPoint = "DisconnectClientByID")]
    internal static extern IntPtr _disconnectClientByID(ulong clientId);

    public IntPtr DisconnectClientByID(ulong clientId) => _disconnectClientByID(clientId);

    [DllImport("nats-bindings.dll", EntryPoint = "GetClientInfo")]
    internal static extern IntPtr _getClientInfo(ulong clientId);

    public IntPtr GetClientInfo(ulong clientId) => _getClientInfo(clientId);

    // Additional monitoring endpoints
    [DllImport("nats-bindings.dll", EntryPoint = "GetAccountz")]
    internal static extern IntPtr _getAccountz(string? accountName);

    public IntPtr GetAccountz(string? accountName) => _getAccountz(accountName);

    [DllImport("nats-bindings.dll", EntryPoint = "GetVarz")]
    internal static extern IntPtr _getVarz();

    public IntPtr GetVarz() => _getVarz();

    [DllImport("nats-bindings.dll", EntryPoint = "GetGatewayz")]
    internal static extern IntPtr _getGatewayz(string? gatewayName);

    public IntPtr GetGatewayz(string? gatewayName) => _getGatewayz(gatewayName);

    // Account management function declarations
    [DllImport("nats-bindings.dll", EntryPoint = "RegisterAccount")]
    internal static extern IntPtr _registerAccount(string accountName);

    public IntPtr RegisterAccount(string accountName) => _registerAccount(accountName);

    [DllImport("nats-bindings.dll", EntryPoint = "LookupAccount")]
    internal static extern IntPtr _lookupAccount(string accountName);

    public IntPtr LookupAccount(string accountName) => _lookupAccount(accountName);

    [DllImport("nats-bindings.dll", EntryPoint = "GetAccountStatz")]
    internal static extern IntPtr _getAccountStatz(string? accountFilter);

    public IntPtr GetAccountStatz(string? accountFilter) => _getAccountStatz(accountFilter);

    // Server state method declarations
    [DllImport("nats-bindings.dll", EntryPoint = "GetServerID")]
    internal static extern IntPtr _getServerID();

    public IntPtr GetServerID() => _getServerID();

    [DllImport("nats-bindings.dll", EntryPoint = "GetServerName")]
    internal static extern IntPtr _getServerName();

    public IntPtr GetServerName() => _getServerName();

    [DllImport("nats-bindings.dll", EntryPoint = "IsServerRunning")]
    internal static extern IntPtr _isServerRunning();

    public IntPtr IsServerRunning() => _isServerRunning();

    [DllImport("nats-bindings.dll", EntryPoint = "WaitForReadyState")]
    internal static extern IntPtr _waitForReadyState(int timeoutSeconds);

    public IntPtr WaitForReadyState(int timeoutSeconds) => _waitForReadyState(timeoutSeconds);

    [DllImport("nats-bindings.dll", EntryPoint = "IsJetStreamEnabled")]
    internal static extern IntPtr _isJetStreamEnabled();

    public IntPtr IsJetStreamEnabled() => _isJetStreamEnabled();
}


internal sealed class LinuxNatsBindings : INatsBindings
{
    // Basic server DllImport declarations
    [DllImport("nats-bindings.so", EntryPoint = "StartServer")]
    internal static extern IntPtr _startServer(string configJson);

    public IntPtr StartServer(string configJson) => _startServer(configJson);

    [DllImport("nats-bindings.so", EntryPoint = "StartServerWithJetStream")]
    internal static extern IntPtr _startServerWithJetStream(string host, int port, string storeDir);

    public IntPtr StartServerWithJetStream(string host, int port, string storeDir) => _startServerWithJetStream(host, port, storeDir);

    [DllImport("nats-bindings.so", EntryPoint = "StartServerFromConfigFile")]
    internal static extern IntPtr _startServerFromConfigFile(string configFilePath);

    public IntPtr StartServerFromConfigFile(string configFilePath) => _startServerFromConfigFile(configFilePath);

    [DllImport("nats-bindings.so", EntryPoint = "ShutdownServer")]
    internal static extern void _shutdownServer();

    public void ShutdownServer() => _shutdownServer();

    [DllImport("nats-bindings.so", EntryPoint = "EnterLameDuckMode")]
    internal static extern IntPtr _enterLameDuckMode();

    public IntPtr EnterLameDuckMode() => _enterLameDuckMode();

    [DllImport("nats-bindings.so", EntryPoint = "SetCurrentPort")]
    internal static extern void _setCurrentPort(int port);

    public void SetCurrentPort(int port) => _setCurrentPort(port);

    [DllImport("nats-bindings.so", EntryPoint = "GetClientURL")]
    internal static extern IntPtr _getClientURL();

    public IntPtr GetClientURL() => _getClientURL();

    [DllImport("nats-bindings.so", EntryPoint = "GetServerInfo")]
    internal static extern IntPtr _getServerInfo();

    public IntPtr GetServerInfo() => _getServerInfo();

    [DllImport("nats-bindings.so", EntryPoint = "FreeString")]
    internal static extern IntPtr _freeString(IntPtr ptr);

    public IntPtr FreeString(IntPtr ptr) => _freeString(ptr);

    // Hot reload function declarations
    [DllImport("nats-bindings.so", EntryPoint = "ReloadConfig")]
    internal static extern IntPtr _reloadConfig();

    public IntPtr ReloadConfig() => _reloadConfig();

    [DllImport("nats-bindings.so", EntryPoint = "ReloadConfigFromFile")]
    internal static extern IntPtr _reloadConfigFromFile(string configFilePath);

    public IntPtr ReloadConfigFromFile(string configFilePath) => _reloadConfigFromFile(configFilePath);

    [DllImport("nats-bindings.so", EntryPoint = "UpdateAndReloadConfig")]
    internal static extern IntPtr _updateAndReloadConfig(string configJson);

    public IntPtr UpdateAndReloadConfig(string configJson) => _updateAndReloadConfig(configJson);

    // Account management declarations
    [DllImport("nats-bindings.so", EntryPoint = "CreateAccount")]
    internal static extern IntPtr _createAccount(string accountJson);

    public IntPtr CreateAccount(string accountJson) => _createAccount(accountJson);

    [DllImport("nats-bindings.so", EntryPoint = "CreateAccountWithJWT")]
    internal static extern IntPtr _createAccountWithJWT(string operatorSeed, string accountConfig);

    public IntPtr CreateAccountWithJWT(string operatorSeed, string accountConfig) => _createAccountWithJWT(operatorSeed, accountConfig);

    // Monitoring endpoints
    [DllImport("nats-bindings.so", EntryPoint = "GetConnz")]
    internal static extern IntPtr _getConnz(string? subsFilter);

    public IntPtr GetConnz(string? subsFilter) => _getConnz(subsFilter);

    [DllImport("nats-bindings.so", EntryPoint = "GetSubsz")]
    internal static extern IntPtr _getSubsz(string? subsFilter);

    public IntPtr GetSubsz(string? subsFilter) => _getSubsz(subsFilter);

    [DllImport("nats-bindings.so", EntryPoint = "GetJsz")]
    internal static extern IntPtr _getJsz(string? accountName);

    public IntPtr GetJsz(string? accountName) => _getJsz(accountName);

    [DllImport("nats-bindings.so", EntryPoint = "GetRoutez")]
    internal static extern IntPtr _getRoutez();

    public IntPtr GetRoutez() => _getRoutez();

    [DllImport("nats-bindings.so", EntryPoint = "GetLeafz")]
    internal static extern IntPtr _getLeafz();

    public IntPtr GetLeafz() => _getLeafz();

    // Connection management
    [DllImport("nats-bindings.so", EntryPoint = "DisconnectClientByID")]
    internal static extern IntPtr _disconnectClientByID(ulong clientId);

    public IntPtr DisconnectClientByID(ulong clientId) => _disconnectClientByID(clientId);

    [DllImport("nats-bindings.so", EntryPoint = "GetClientInfo")]
    internal static extern IntPtr _getClientInfo(ulong clientId);

    public IntPtr GetClientInfo(ulong clientId) => _getClientInfo(clientId);

    // Additional monitoring endpoints
    [DllImport("nats-bindings.so", EntryPoint = "GetAccountz")]
    internal static extern IntPtr _getAccountz(string? accountName);

    public IntPtr GetAccountz(string? accountName) => _getAccountz(accountName);

    [DllImport("nats-bindings.so", EntryPoint = "GetVarz")]
    internal static extern IntPtr _getVarz();

    public IntPtr GetVarz() => _getVarz();

    [DllImport("nats-bindings.so", EntryPoint = "GetGatewayz")]
    internal static extern IntPtr _getGatewayz(string? gatewayName);

    public IntPtr GetGatewayz(string? gatewayName) => _getGatewayz(gatewayName);

    // Account management function declarations
    [DllImport("nats-bindings.so", EntryPoint = "RegisterAccount")]
    internal static extern IntPtr _registerAccount(string accountName);

    public IntPtr RegisterAccount(string accountName) => _registerAccount(accountName);

    [DllImport("nats-bindings.so", EntryPoint = "LookupAccount")]
    internal static extern IntPtr _lookupAccount(string accountName);

    public IntPtr LookupAccount(string accountName) => _lookupAccount(accountName);

    [DllImport("nats-bindings.so", EntryPoint = "GetAccountStatz")]
    internal static extern IntPtr _getAccountStatz(string? accountFilter);

    public IntPtr GetAccountStatz(string? accountFilter) => _getAccountStatz(accountFilter);

    // Server state method declarations
    [DllImport("nats-bindings.so", EntryPoint = "GetServerID")]
    internal static extern IntPtr _getServerID();

    public IntPtr GetServerID() => _getServerID();

    [DllImport("nats-bindings.so", EntryPoint = "GetServerName")]
    internal static extern IntPtr _getServerName();

    public IntPtr GetServerName() => _getServerName();

    [DllImport("nats-bindings.so", EntryPoint = "IsServerRunning")]
    internal static extern IntPtr _isServerRunning();

    public IntPtr IsServerRunning() => _isServerRunning();

    [DllImport("nats-bindings.so", EntryPoint = "WaitForReadyState")]
    internal static extern IntPtr _waitForReadyState(int timeoutSeconds);

    public IntPtr WaitForReadyState(int timeoutSeconds) => _waitForReadyState(timeoutSeconds);

    [DllImport("nats-bindings.so", EntryPoint = "IsJetStreamEnabled")]
    internal static extern IntPtr _isJetStreamEnabled();

    public IntPtr IsJetStreamEnabled() => _isJetStreamEnabled();
}
