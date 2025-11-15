using MessageBroker.Examples.BasicUsage;
using MessageBroker.Examples.HotReload;
using MessageBroker.Examples.Advanced;
using MessageBroker.Examples.Monitoring;

namespace MessageBroker.Examples;

/// <summary>
/// Interactive console application demonstrating all key features of MessageBroker.NET.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Check for command-line arguments
        if (args.Length > 0 && args[0] == "test")
        {
            await SimpleTest.RunAsync();
            return;
        }

        try { Console.Clear(); } catch { /* Ignore console errors in non-interactive mode */ }
        ShowWelcomeBanner();

        bool running = true;

        while (running)
        {
            ShowMenu();
            var choice = Console.ReadKey(true).KeyChar;

            try { Console.Clear(); } catch { /* Ignore console errors in non-interactive mode */ }

            switch (choice)
            {
                case '1':
                    await BasicServerExample.RunAsync();
                    break;

                case '2':
                    await ConfigurationReloadExample.RunAsync();
                    break;

                case '3':
                    await ValidationExample.RunAsync();
                    break;

                case '4':
                    await RollbackExample.RunAsync();
                    break;

                case '5':
                    await ChangeNotificationExample.RunAsync();
                    break;

                case '6':
                    await FluentApiExample.RunAsync();
                    break;

                case '7':
                    await CompleteWorkflowExample.RunAsync();
                    break;

                case '8':
                    await LameDuckModeExample.RunAsync();
                    break;

                case '9':
                    await MonitoringExample.RunAsync();
                    break;

                case 'a':
                case 'A':
                    await ClusterMonitoringExample.RunAsync();
                    break;

                case 'b':
                case 'B':
                    await ClientManagementExample.RunAsync();
                    break;

                case 'q':
                case 'Q':
                case '0':
                    running = false;
                    ShowGoodbye();
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\nInvalid choice. Please select 1-9, A-B, or Q to quit.");
                    Console.ResetColor();
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey(true);
                    break;
            }

            try { Console.Clear(); } catch { /* Ignore console errors in non-interactive mode */ }
        }
    }

    static void ShowWelcomeBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║               MessageBroker.NET Examples                     ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║     Enhanced Runtime Configuration for NATS Messaging       ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Welcome! These examples demonstrate the enhanced capabilities");
        Console.WriteLine("of MessageBroker.NET compared to the original nats-csharp.");
        Console.WriteLine();
    }

    static void ShowMenu()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("                        MAIN MENU                              ");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  BASIC USAGE");
        Console.ResetColor();
        Console.WriteLine("  1. Basic Server Startup");
        Console.WriteLine("     └─ Simple server lifecycle management");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  HOT RELOAD FEATURES");
        Console.ResetColor();
        Console.WriteLine("  2. Hot Configuration Reload");
        Console.WriteLine("     └─ Change settings without restarting");
        Console.WriteLine("  3. Configuration Validation");
        Console.WriteLine("     └─ Prevent invalid configurations");
        Console.WriteLine("  4. Rollback Example");
        Console.WriteLine("     └─ Recover from problematic changes");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  ADVANCED FEATURES");
        Console.ResetColor();
        Console.WriteLine("  5. Change Notifications");
        Console.WriteLine("     └─ Event-driven configuration monitoring");
        Console.WriteLine("  6. Fluent API Usage");
        Console.WriteLine("     └─ Chainable configuration methods");
        Console.WriteLine("  7. Complete Workflow");
        Console.WriteLine("     └─ Production-ready end-to-end example");
        Console.WriteLine("  8. Lame Duck Mode");
        Console.WriteLine("     └─ Graceful shutdown with connection draining");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  MONITORING & OBSERVABILITY");
        Console.ResetColor();
        Console.WriteLine("  9. Server Monitoring");
        Console.WriteLine("     └─ Connz, Subsz, Jsz endpoints");
        Console.WriteLine("  A. Cluster Monitoring");
        Console.WriteLine("     └─ Routez and Leafz endpoints");
        Console.WriteLine("  B. Client Management");
        Console.WriteLine("     └─ Connection tracking and disconnection");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("  OTHER");
        Console.ResetColor();
        Console.WriteLine("  Q. Exit");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.Write("\nSelect an option (1-9, A-B, Q): ");
    }

    static void ShowGoodbye()
    {
        try { Console.Clear(); } catch { /* Ignore console errors in non-interactive mode */ }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║                  Thanks for exploring!                       ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("║              MessageBroker.NET Examples                      ║");
        Console.WriteLine("║                                                              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Key Features Demonstrated:");
        Console.WriteLine("  ✓ Hot configuration reload without downtime");
        Console.WriteLine("  ✓ Configuration validation and error prevention");
        Console.WriteLine("  ✓ Version history and rollback capabilities");
        Console.WriteLine("  ✓ Event-driven change notifications");
        Console.WriteLine("  ✓ Fluent API for clean, readable code");
        Console.WriteLine("  ✓ Lame duck mode for graceful shutdowns");
        Console.WriteLine("  ✓ Server monitoring (Connz, Subsz, Jsz)");
        Console.WriteLine("  ✓ Cluster and leaf node monitoring");
        Console.WriteLine("  ✓ Client connection management");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("For more information, see ComparisonWithNatsSharp.md");
        Console.ResetColor();
        Console.WriteLine();
    }
}
