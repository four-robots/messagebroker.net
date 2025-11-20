using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests leaf node configuration including hot reload of import/export subjects.
/// </summary>
public class LeafNodeConfigurationTests
{
    [Fact]
    public async Task ConfigureLeafNodeWithImportAndExportSubjects()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string> { "events.>", "data.*" },
                ExportSubjects = new List<string> { "commands.>", "status.*" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        var leafNode = info.CurrentConfig.LeafNode;

        Assert.Equal(2, leafNode.ImportSubjects.Count);
        Assert.Contains("events.>", leafNode.ImportSubjects);
        Assert.Contains("data.*", leafNode.ImportSubjects);

        Assert.Equal(2, leafNode.ExportSubjects.Count);
        Assert.Contains("commands.>", leafNode.ExportSubjects);
        Assert.Contains("status.*", leafNode.ExportSubjects);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HotReloadAddImportSubjectsToLeafNode()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string> { "events.>" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        // Hot reload to add more import subjects
        var result = await server.AddLeafNodeImportSubjectsAsync("data.*", "logs.>");

        Assert.True(result.Success, "Failed to add import subjects");

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        var importSubjects = info.CurrentConfig.LeafNode.ImportSubjects;

        Assert.Equal(3, importSubjects.Count);
        Assert.Contains("events.>", importSubjects);
        Assert.Contains("data.*", importSubjects);
        Assert.Contains("logs.>", importSubjects);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HotReloadRemoveImportSubjectsFromLeafNode()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string> { "events.>", "data.*", "logs.>" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        // Hot reload to remove an import subject
        var result = await server.RemoveLeafNodeImportSubjectsAsync("data.*");

        Assert.True(result.Success, "Failed to remove import subject");

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        var importSubjects = info.CurrentConfig.LeafNode.ImportSubjects;

        Assert.Equal(2, importSubjects.Count);
        Assert.Contains("events.>", importSubjects);
        Assert.Contains("logs.>", importSubjects);
        Assert.DoesNotContain("data.*", importSubjects);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HotReloadAddExportSubjectsToLeafNode()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ExportSubjects = new List<string> { "commands.>" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        var result = await server.AddLeafNodeExportSubjectsAsync("status.*", "metrics.>");

        Assert.True(result.Success, "Failed to add export subjects");

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        var exportSubjects = info.CurrentConfig.LeafNode.ExportSubjects;

        Assert.Equal(3, exportSubjects.Count);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HotReloadReplaceAllImportSubjects()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string> { "old.>", "legacy.*" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        var result = await server.SetLeafNodeImportSubjectsAsync(new[] { "new.>", "modern.*" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success, "Failed to replace import subjects");

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        var importSubjects = info.CurrentConfig.LeafNode.ImportSubjects;

        Assert.Equal(2, importSubjects.Count);
        Assert.Contains("new.>", importSubjects);
        Assert.Contains("modern.*", importSubjects);
        Assert.DoesNotContain("old.>", importSubjects);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HotReloadReplaceAllExportSubjects()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ExportSubjects = new List<string> { "old.>", "legacy.*" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        var result = await server.SetLeafNodeExportSubjectsAsync(new[] { "new.>", "modern.*" }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success, "Failed to replace export subjects");

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        var exportSubjects = info.CurrentConfig.LeafNode.ExportSubjects;

        Assert.Equal(2, exportSubjects.Count);
        Assert.Contains("new.>", exportSubjects);
        Assert.Contains("modern.*", exportSubjects);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MultipleSequentialHotReloadsOfLeafNodeSubjects()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string> { "v1.>" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        // First reload - add subjects
        await server.AddLeafNodeImportSubjectsAsync("v2.>");

        // Second reload - add more subjects
        await server.AddLeafNodeImportSubjectsAsync("v3.>");

        // Third reload - remove one
        await server.RemoveLeafNodeImportSubjectsAsync("v1.>");

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        var importSubjects = info.CurrentConfig.LeafNode.ImportSubjects;

        Assert.Equal(2, importSubjects.Count);
        Assert.Contains("v2.>", importSubjects);
        Assert.Contains("v3.>", importSubjects);
        Assert.DoesNotContain("v1.>", importSubjects);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LeafNodeWithWildcardPatterns()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string>
                {
                    "events.>",          // Multi-token wildcard
                    "data.*.received",   // Single-token wildcard
                    "logs.*.*.error",    // Multiple single-token wildcards
                    ">"                  // Full wildcard
                }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(4, info.CurrentConfig.LeafNode.ImportSubjects.Count);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
    }
}
