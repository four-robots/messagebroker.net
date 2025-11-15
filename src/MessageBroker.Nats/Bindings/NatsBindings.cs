
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
}
