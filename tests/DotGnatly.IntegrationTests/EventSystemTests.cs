using DotGnatly.Core.Configuration;
using DotGnatly.Core.Events;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests the event system for configuration changes.
/// </summary>
public class EventSystemTests
{
    [Fact]
    public async Task TestConfigurationChangingEventFires()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        bool eventFired = false;
        server.ConfigurationChanging += (sender, args) =>
        {
            eventFired = true;
        };

        await server.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.True(eventFired, "ConfigurationChanging event should have fired");
    }

    [Fact]
    public async Task TestConfigurationChangedEventFires()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        bool eventFired = false;
        server.ConfigurationChanged += (sender, args) =>
        {
            eventFired = true;
        };

        await server.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.True(eventFired, "ConfigurationChanged event should have fired");
    }

    [Fact]
    public async Task TestConfigurationChangingCanCancelChanges()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222, Debug = false }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        server.ConfigurationChanging += (sender, args) =>
        {
            args.CancelChange("Test cancellation");
        };

        var changeResult = await server.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.False(changeResult.Success, "Change should have been cancelled");
        Assert.False(info.CurrentConfig.Debug, "Debug should still be false");
    }

    [Fact]
    public async Task TestConfigurationChangedEventProvidesCorrectDiff()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222, Debug = false }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        ConfigurationChangedEventArgs? capturedArgs = null;
        server.ConfigurationChanged += (sender, args) =>
        {
            capturedArgs = args;
        };

        await server.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(capturedArgs);
        Assert.True(capturedArgs.Diff.Changes.Any(ch => ch.PropertyName == "Debug"), "Diff should contain Debug property change");
    }

    [Fact]
    public async Task TestMultipleEventHandlersAllFire()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        int handler1Count = 0;
        int handler2Count = 0;
        int handler3Count = 0;

        server.ConfigurationChanged += (s, e) => handler1Count++;
        server.ConfigurationChanged += (s, e) => handler2Count++;
        server.ConfigurationChanged += (s, e) => handler3Count++;

        await server.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, handler1Count);
        Assert.Equal(1, handler2Count);
        Assert.Equal(1, handler3Count);
    }

    [Fact]
    public async Task TestEventProvidesAccessToOldAndNewConfigurations()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222, MaxPayload = 1024 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        bool correctValues = false;
        server.ConfigurationChanging += (sender, args) =>
        {
            correctValues = args.Current.MaxPayload == 1024 &&
                           args.Proposed.MaxPayload == 2048;
        };

        await server.ApplyChangesAsync(c => c.MaxPayload = 2048, TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.True(correctValues, "Event should provide correct old and new values");
    }

    [Fact]
    public async Task TestEventsFireForLeafNodeSubjectChanges()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string> { "old.>" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        bool changingFired = false;
        bool changedFired = false;

        server.ConfigurationChanging += (s, e) => changingFired = true;
        server.ConfigurationChanged += (s, e) => changedFired = true;

        await server.AddLeafNodeImportSubjectsAsync("new.>");
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.True(changingFired, "ConfigurationChanging should have fired");
        Assert.True(changedFired, "ConfigurationChanged should have fired");
    }

    [Fact]
    public async Task TestEventCancellationPreventsConfigurationChange()
    {
        using var server = new NatsController();
        var configResult = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                ImportSubjects = new List<string> { "original.>" }
            }
        }, TestContext.Current.CancellationToken);

        Assert.True(configResult.Success, $"Configuration failed: {configResult.ErrorMessage}");

        server.ConfigurationChanging += (sender, args) =>
        {
            args.CancelChange("Not allowed");
        };

        await server.SetLeafNodeImportSubjectsAsync(new[] { "modified.>" }, cancellationToken: TestContext.Current.CancellationToken);

        var info = await server.GetInfoAsync(TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Contains("original.>", info.CurrentConfig.LeafNode.ImportSubjects);
        Assert.DoesNotContain("modified.>", info.CurrentConfig.LeafNode.ImportSubjects);
    }

    [Fact]
    public async Task TestEventsFireInCorrectOrder()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        var eventOrder = new List<string>();

        server.ConfigurationChanging += (s, e) => eventOrder.Add("Changing");
        server.ConfigurationChanged += (s, e) => eventOrder.Add("Changed");

        await server.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);
        await server.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, eventOrder.Count);
        Assert.Equal("Changing", eventOrder[0]);
        Assert.Equal("Changed", eventOrder[1]);
    }

    [Fact]
    public async Task TestEventsFireIndependentlyForMultipleServers()
    {
        using var server1 = new NatsController();
        using var server2 = new NatsController();

        var result1 = await server1.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);
        var result2 = await server2.ConfigureAsync(new BrokerConfiguration { Port = 14223 }, TestContext.Current.CancellationToken);

        Assert.True(result1.Success, $"Failed to start server1: {result1.ErrorMessage}");
        Assert.True(result2.Success, $"Failed to start server2: {result2.ErrorMessage}");

        int server1Events = 0;
        int server2Events = 0;

        server1.ConfigurationChanged += (s, e) => server1Events++;
        server2.ConfigurationChanged += (s, e) => server2Events++;

        await server1.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken); // Small delay to ensure event processing

        await server1.ShutdownAsync(TestContext.Current.CancellationToken);
        await server2.ShutdownAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, server1Events);
        Assert.Equal(0, server2Events);
    }
}
