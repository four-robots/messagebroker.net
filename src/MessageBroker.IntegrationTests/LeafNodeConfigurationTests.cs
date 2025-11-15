using MessageBroker.Core.Configuration;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.IntegrationTests;

/// <summary>
/// Tests leaf node configuration including hot reload of import/export subjects.
/// </summary>
public class LeafNodeConfigurationTests : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        // Test 1: Configure leaf node with import/export subjects
        await results.AssertNoExceptionAsync(
            "Configure leaf node with import and export subjects",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string> { "events.>", "data.*" },
                        ExportSubjects = new List<string> { "commands.>", "status.*" }
                    }
                });

                var info = await server.GetInfoAsync();
                var leafNode = info.CurrentConfig.LeafNode;

                if (leafNode.ImportSubjects.Count != 2 ||
                    !leafNode.ImportSubjects.Contains("events.>") ||
                    !leafNode.ImportSubjects.Contains("data.*"))
                {
                    throw new Exception("Import subjects not configured correctly");
                }

                if (leafNode.ExportSubjects.Count != 2 ||
                    !leafNode.ExportSubjects.Contains("commands.>") ||
                    !leafNode.ExportSubjects.Contains("status.*"))
                {
                    throw new Exception("Export subjects not configured correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 2: Hot reload - Add import subjects
        await results.AssertNoExceptionAsync(
            "Hot reload: Add import subjects to leaf node",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string> { "events.>" }
                    }
                });

                // Hot reload to add more import subjects
                var result = await server.AddLeafNodeImportSubjectsAsync("data.*", "logs.>");

                if (!result.Success)
                {
                    throw new Exception("Failed to add import subjects");
                }

                var info = await server.GetInfoAsync();
                var importSubjects = info.CurrentConfig.LeafNode.ImportSubjects;

                if (importSubjects.Count != 3 ||
                    !importSubjects.Contains("events.>") ||
                    !importSubjects.Contains("data.*") ||
                    !importSubjects.Contains("logs.>"))
                {
                    throw new Exception("Import subjects not updated correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 3: Hot reload - Remove import subjects
        await results.AssertNoExceptionAsync(
            "Hot reload: Remove import subjects from leaf node",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string> { "events.>", "data.*", "logs.>" }
                    }
                });

                // Hot reload to remove an import subject
                var result = await server.RemoveLeafNodeImportSubjectsAsync("data.*");

                if (!result.Success)
                {
                    throw new Exception("Failed to remove import subject");
                }

                var info = await server.GetInfoAsync();
                var importSubjects = info.CurrentConfig.LeafNode.ImportSubjects;

                if (importSubjects.Count != 2 ||
                    !importSubjects.Contains("events.>") ||
                    !importSubjects.Contains("logs.>") ||
                    importSubjects.Contains("data.*"))
                {
                    throw new Exception("Import subjects not updated correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 4: Hot reload - Add export subjects
        await results.AssertNoExceptionAsync(
            "Hot reload: Add export subjects to leaf node",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ExportSubjects = new List<string> { "commands.>" }
                    }
                });

                var result = await server.AddLeafNodeExportSubjectsAsync("status.*", "metrics.>");

                if (!result.Success)
                {
                    throw new Exception("Failed to add export subjects");
                }

                var info = await server.GetInfoAsync();
                var exportSubjects = info.CurrentConfig.LeafNode.ExportSubjects;

                if (exportSubjects.Count != 3)
                {
                    throw new Exception($"Expected 3 export subjects, got {exportSubjects.Count}");
                }

                await server.ShutdownAsync();
            });

        // Test 5: Hot reload - Replace all import subjects
        await results.AssertNoExceptionAsync(
            "Hot reload: Replace all import subjects",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string> { "old.>", "legacy.*" }
                    }
                });

                var result = await server.SetLeafNodeImportSubjectsAsync(new[] { "new.>", "modern.*" });

                if (!result.Success)
                {
                    throw new Exception("Failed to replace import subjects");
                }

                var info = await server.GetInfoAsync();
                var importSubjects = info.CurrentConfig.LeafNode.ImportSubjects;

                if (importSubjects.Count != 2 ||
                    !importSubjects.Contains("new.>") ||
                    !importSubjects.Contains("modern.*") ||
                    importSubjects.Contains("old.>"))
                {
                    throw new Exception("Import subjects not replaced correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 6: Hot reload - Replace all export subjects
        await results.AssertNoExceptionAsync(
            "Hot reload: Replace all export subjects",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ExportSubjects = new List<string> { "old.>", "legacy.*" }
                    }
                });

                var result = await server.SetLeafNodeExportSubjectsAsync(new[] { "new.>", "modern.*" });

                if (!result.Success)
                {
                    throw new Exception("Failed to replace export subjects");
                }

                var info = await server.GetInfoAsync();
                var exportSubjects = info.CurrentConfig.LeafNode.ExportSubjects;

                if (exportSubjects.Count != 2 ||
                    !exportSubjects.Contains("new.>") ||
                    !exportSubjects.Contains("modern.*"))
                {
                    throw new Exception("Export subjects not replaced correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 7: Multiple hot reloads in sequence
        await results.AssertNoExceptionAsync(
            "Multiple sequential hot reloads of leaf node subjects",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string> { "v1.>" }
                    }
                });

                // First reload - add subjects
                await server.AddLeafNodeImportSubjectsAsync("v2.>");

                // Second reload - add more subjects
                await server.AddLeafNodeImportSubjectsAsync("v3.>");

                // Third reload - remove one
                await server.RemoveLeafNodeImportSubjectsAsync("v1.>");

                var info = await server.GetInfoAsync();
                var importSubjects = info.CurrentConfig.LeafNode.ImportSubjects;

                if (importSubjects.Count != 2 ||
                    !importSubjects.Contains("v2.>") ||
                    !importSubjects.Contains("v3.>") ||
                    importSubjects.Contains("v1.>"))
                {
                    throw new Exception("Sequential hot reloads did not work correctly");
                }

                await server.ShutdownAsync();
            });

        // Test 8: Wildcard patterns in subjects
        await results.AssertNoExceptionAsync(
            "Leaf node with wildcard patterns (* and >)",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string>
                        {
                            "events.>",          // Multi-token wildcard
                            "data.*.received",   // Single-token wildcard
                            "logs.*.*.error",    // Multiple single-token wildcards
                            ">"                  // Full wildcard
                        }
                    }
                });

                var info = await server.GetInfoAsync();

                if (info.CurrentConfig.LeafNode.ImportSubjects.Count != 4)
                {
                    throw new Exception("Wildcard patterns not configured correctly");
                }

                await server.ShutdownAsync();
            });
    }
}
