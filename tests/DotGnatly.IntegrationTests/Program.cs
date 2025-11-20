using DotGnatly.IntegrationTests;

Console.WriteLine("========================================");
Console.WriteLine("MessageBroker.NET Integration Tests");
Console.WriteLine("========================================");
Console.WriteLine();

var testRunner = new IntegrationTestRunner();
var results = await testRunner.RunAllTestsAsync();

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
