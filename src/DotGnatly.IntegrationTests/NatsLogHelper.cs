using DotGnatly.Nats.Implementation;
using System.Runtime.InteropServices;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Helper class for NATS server log streaming in integration tests.
/// Provides lock-free log capture via Unix domain sockets.
/// </summary>
public static class NatsLogHelper
{
    private static NatsLogStreamReader? _logStreamReader;
    private static readonly object _lock = new();
    private static bool _isVerbose = false;

    /// <summary>
    /// Initializes log streaming for verbose test output.
    /// Should be called once at the start of test execution.
    /// </summary>
    public static void Initialize(bool verbose)
    {
        lock (_lock)
        {
            _isVerbose = verbose;

            if (verbose && _logStreamReader == null)
            {
                // Only create log stream reader on supported platforms
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    try
                    {
                        _logStreamReader = new NatsLogStreamReader();

                        // Forward logs to console in verbose mode
                        _logStreamReader.LogReceived += log =>
                        {
                            Console.WriteLine($"[NATS] {log}");
                        };
                    }
                    catch
                    {
                        // If log streaming setup fails, just continue without it
                        _logStreamReader = null;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Enables log streaming for a specific NatsController instance.
    /// Call this after creating a controller but before configuring it.
    /// </summary>
    public static void EnableLogging(NatsController controller)
    {
        lock (_lock)
        {
            if (_logStreamReader != null && _isVerbose)
            {
                try
                {
                    var result = controller.SetupLogPipe(_logStreamReader.PipePath);
                    if (result.StartsWith("ERROR"))
                    {
                        Console.WriteLine($"Warning: Failed to setup log pipe: {result}");
                    }
                }
                catch
                {
                    // Silently ignore if log setup fails
                }
            }
        }
    }

    /// <summary>
    /// Gets all captured log lines (only if verbose mode is enabled).
    /// </summary>
    public static IReadOnlyList<string> GetLogLines()
    {
        lock (_lock)
        {
            return _logStreamReader?.LogLines ?? new List<string>();
        }
    }

    /// <summary>
    /// Cleanup log streaming resources.
    /// Should be called at the end of test execution.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            _logStreamReader?.Dispose();
            _logStreamReader = null;
            _isVerbose = false;
        }
    }
}
