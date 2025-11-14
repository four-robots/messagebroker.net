using MessageBroker.Core.Configuration;
using MessageBroker.Core.Events;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.IntegrationTests;

/// <summary>
/// Tests the event system for configuration changes.
/// </summary>
public class EventSystemTests : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        // Test 1: ConfigurationChanging event fires
        await results.AssertAsync(
            "ConfigurationChanging event fires before changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                bool eventFired = false;
                server.ConfigurationChanging += (sender, args) =>
                {
                    eventFired = true;
                };

                await server.ApplyChangesAsync(c => c.Debug = true);
                await server.ShutdownAsync();

                return eventFired;
            });

        // Test 2: ConfigurationChanged event fires
        await results.AssertAsync(
            "ConfigurationChanged event fires after changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                bool eventFired = false;
                server.ConfigurationChanged += (sender, args) =>
                {
                    eventFired = true;
                };

                await server.ApplyChangesAsync(c => c.Debug = true);
                await server.ShutdownAsync();

                return eventFired;
            });

        // Test 3: Cancel configuration change via event
        await results.AssertAsync(
            "ConfigurationChanging event can cancel changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222, Debug = false });

                server.ConfigurationChanging += (sender, args) =>
                {
                    args.CancelChange("Test cancellation");
                };

                var result = await server.ApplyChangesAsync(c => c.Debug = true);

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return !result.Success && info.CurrentConfiguration.Debug == false;
            });

        // Test 4: Event provides correct diff information
        await results.AssertAsync(
            "ConfigurationChanged event provides correct diff",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222, Debug = false });

                ConfigurationChangedEventArgs? capturedArgs = null;
                server.ConfigurationChanged += (sender, args) =>
                {
                    capturedArgs = args;
                };

                await server.ApplyChangesAsync(c => c.Debug = true);
                await server.ShutdownAsync();

                return capturedArgs != null &&
                       capturedArgs.Diff.Changes.Any(ch => ch.PropertyName == "Debug");
            });

        // Test 5: Multiple event handlers
        await results.AssertAsync(
            "Multiple event handlers all fire",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                int handler1Count = 0;
                int handler2Count = 0;
                int handler3Count = 0;

                server.ConfigurationChanged += (s, e) => handler1Count++;
                server.ConfigurationChanged += (s, e) => handler2Count++;
                server.ConfigurationChanged += (s, e) => handler3Count++;

                await server.ApplyChangesAsync(c => c.Debug = true);
                await server.ShutdownAsync();

                return handler1Count == 1 && handler2Count == 1 && handler3Count == 1;
            });

        // Test 6: Event handler can access old and new values
        await results.AssertAsync(
            "Event provides access to old and new configurations",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222, MaxPayload = 1024 });

                bool correctValues = false;
                server.ConfigurationChanging += (sender, args) =>
                {
                    correctValues = args.Current.MaxPayload == 1024 &&
                                   args.Proposed.MaxPayload == 2048;
                };

                await server.ApplyChangesAsync(c => c.MaxPayload = 2048);
                await server.ShutdownAsync();

                return correctValues;
            });

        // Test 7: Events fire for leaf node subject changes
        await results.AssertAsync(
            "Events fire for leaf node subject changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string> { "old.>" }
                    }
                });

                bool changingFired = false;
                bool changedFired = false;

                server.ConfigurationChanging += (s, e) => changingFired = true;
                server.ConfigurationChanged += (s, e) => changedFired = true;

                await server.AddLeafNodeImportSubjectsAsync("new.>");
                await server.ShutdownAsync();

                return changingFired && changedFired;
            });

        // Test 8: Event cancellation prevents configuration change
        await results.AssertAsync(
            "Cancelled configuration remains unchanged",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        ImportSubjects = new List<string> { "original.>" }
                    }
                });

                server.ConfigurationChanging += (sender, args) =>
                {
                    args.CancelChange("Not allowed");
                };

                await server.SetLeafNodeImportSubjectsAsync(new[] { "modified.>" });

                var info = await server.GetServerInfoAsync();
                await server.ShutdownAsync();

                return info.CurrentConfiguration.LeafNode.ImportSubjects.Contains("original.>") &&
                       !info.CurrentConfiguration.LeafNode.ImportSubjects.Contains("modified.>");
            });

        // Test 9: Events fire in correct order
        await results.AssertAsync(
            "Events fire in correct order (Changing then Changed)",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                var eventOrder = new List<string>();

                server.ConfigurationChanging += (s, e) => eventOrder.Add("Changing");
                server.ConfigurationChanged += (s, e) => eventOrder.Add("Changed");

                await server.ApplyChangesAsync(c => c.Debug = true);
                await server.ShutdownAsync();

                return eventOrder.Count == 2 &&
                       eventOrder[0] == "Changing" &&
                       eventOrder[1] == "Changed";
            });

        // Test 10: Events for multiple concurrent servers
        await results.AssertAsync(
            "Events fire independently for multiple servers",
            async () =>
            {
                using var server1 = new NatsController();
                using var server2 = new NatsController();

                await server1.ConfigureAsync(new BrokerConfiguration { Port = 4222 });
                await server2.ConfigureAsync(new BrokerConfiguration { Port = 4223 });

                int server1Events = 0;
                int server2Events = 0;

                server1.ConfigurationChanged += (s, e) => server1Events++;
                server2.ConfigurationChanged += (s, e) => server2Events++;

                await server1.ApplyChangesAsync(c => c.Debug = true);

                await Task.Delay(50); // Small delay to ensure event processing

                await server1.ShutdownAsync();
                await server2.ShutdownAsync();

                return server1Events == 1 && server2Events == 0;
            });
    }
}
