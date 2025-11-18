using DotGnatly.IntegrationTests;

// Parse command-line arguments
bool verbose = args.Contains("--verbose") || args.Contains("-v");

Console.WriteLine("========================================");
Console.WriteLine("MessageBroker.NET Integration Tests");
Console.WriteLine("========================================");
Console.WriteLine();

if (verbose)
{
    Console.WriteLine("Running in VERBOSE mode - showing all test output");
    Console.WriteLine();
}

// Initialize NATS log streaming (if verbose and on supported platform)
NatsLogHelper.Initialize(verbose);

try
{
    var testRunner = new IntegrationTestRunner();
    var results = await testRunner.RunAllTestsAsync(verbose);

    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine("Test Results Summary");
    Console.WriteLine("========================================");
    Console.WriteLine($"Total Tests: {results.TotalTests}");
    Console.WriteLine($"Passed: {results.PassedTests}");
    Console.WriteLine($"Failed: {results.FailedTests}");
    Console.WriteLine($"Success Rate: {results.SuccessRate:F1}%");
    Console.WriteLine();

    if (results.FailedTests > 0)
    {
        Console.WriteLine("Failed Tests:");
        foreach (var failure in results.Failures)
        {
            Console.WriteLine($"  - {failure}");
        }
        Environment.Exit(1);
    }
    else
    {
        Console.WriteLine("✓ All integration tests passed!");
        Environment.Exit(0);
    }
}
finally
{
    // Cleanup NATS log streaming
    NatsLogHelper.Shutdown();
}
