using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Captures NATS server logs via Unix domain socket (lock-free streaming from Go).
/// Used for verbose test output and debugging.
/// </summary>
public sealed class NatsLogStreamReader : IDisposable
{
    private readonly string _pipePath;
    private readonly Socket? _listenerSocket;
    private Socket? _clientSocket;
    private readonly CancellationTokenSource _cts;
    private readonly Task _readTask;
    private readonly List<string> _logLines = new();
    private readonly object _logLock = new();

    public event Action<string>? LogReceived;

    public NatsLogStreamReader()
    {
        // Create unique pipe path in temp directory
        _pipePath = Path.Combine(Path.GetTempPath(), $"nats-log-{Guid.NewGuid()}.sock");

        _cts = new CancellationTokenSource();

        // Only create Unix socket on Linux/macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Delete any existing socket file
            if (File.Exists(_pipePath))
                File.Delete(_pipePath);

            // Create Unix domain socket
            _listenerSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(_pipePath);
            _listenerSocket.Bind(endpoint);
            _listenerSocket.Listen(1);

            // Start task to accept connection and read logs
            _readTask = Task.Run(() => AcceptAndReadLogsAsync(_cts.Token));
        }
        else
        {
            // Windows - not implemented yet (could use Named Pipes)
            throw new PlatformNotSupportedException("Log streaming currently only supported on Linux/macOS");
        }
    }

    public string PipePath => _pipePath;

    public IReadOnlyList<string> LogLines
    {
        get
        {
            lock (_logLock)
            {
                return _logLines.ToList();
            }
        }
    }

    private async Task AcceptAndReadLogsAsync(CancellationToken ct)
    {
        if (_listenerSocket == null)
            return;

        try
        {
            // Wait for Go side to connect (with timeout)
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            _clientSocket = await _listenerSocket.AcceptAsync(timeoutCts.Token);

            // Read logs line by line
            using var stream = new NetworkStream(_clientSocket, ownsSocket: false);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null)
                    break; // Connection closed

                // Store log line
                lock (_logLock)
                {
                    _logLines.Add(line);
                }

                // Fire event
                LogReceived?.Invoke(line);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception)
        {
            // Socket error or timeout - silently ignore
            // This is expected if no logs are produced or server doesn't start
        }
    }

    public void Dispose()
    {
        // Cancel reading
        _cts.Cancel();

        // Close sockets
        _clientSocket?.Close();
        _listenerSocket?.Close();

        // Wait for read task to complete (with timeout)
        try
        {
            _readTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Ignore timeout
        }

        // Clean up socket file
        try
        {
            if (File.Exists(_pipePath))
                File.Delete(_pipePath);
        }
        catch
        {
            // Ignore cleanup errors
        }

        _cts.Dispose();
    }
}
