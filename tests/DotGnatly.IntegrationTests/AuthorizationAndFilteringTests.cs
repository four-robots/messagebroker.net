using System.Text;
using System.Text.Json;
using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using NATS.Net;
using NATS.Client.Core;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Integration tests for NATS server account authorization and subject filtering.
/// Tests client connectivity using NATS.Net package.
/// </summary>
public class AuthorizationAndFilteringTests
{
    /// <summary>
    /// Tests basic username/password authentication.
    /// </summary>
    [Fact]
    public async Task TestBasicAuthentication()
    {
        Console.WriteLine("\n=== Testing Basic Authentication ===");

        using var controller = new NatsController();

        // Start server with basic authentication
        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4250,
            Description = "Basic authentication test",
            Auth = new AuthConfiguration
            {
                Username = "testuser",
                Password = "testpass123"
            }
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Try to connect with correct credentials using NatsOpts
            var opts = new NatsOpts
            {
                Url = "nats://127.0.0.1:4250",
                AuthOpts = new NatsAuthOpts
                {
                    Username = "testuser",
                    Password = "testpass123"
                }
            };
            await using var nats = new NatsClient(opts);
            Console.WriteLine("✓ Connected with valid credentials");

            // Verify connection by publishing and subscribing
            var receivedMessages = new List<string>();
            var subscription = nats.SubscribeAsync<string>("test.auth", cancellationToken: TestContext.Current.CancellationToken);

            var subscriptionTask = Task.Run(async () =>
            {
                await foreach (var msg in subscription)
                {
                    receivedMessages.Add(msg.Data ?? "");
                    if (receivedMessages.Count >= 3)
                        break;
                }
            }, TestContext.Current.CancellationToken);

            await Task.Delay(500, TestContext.Current.CancellationToken); // Give subscription time to establish

            await nats.PublishAsync("test.auth", "Message 1", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("test.auth", "Message 2", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("test.auth", "Message 3", cancellationToken: TestContext.Current.CancellationToken);

            var timeoutTask = Task.Delay(5000, TestContext.Current.CancellationToken);
            var completedTask = await Task.WhenAny(subscriptionTask, timeoutTask);

            Assert.NotEqual(timeoutTask, completedTask);
            Assert.Equal(3, receivedMessages.Count);

            Console.WriteLine("✓ Successfully published and received 3 messages");
            Console.WriteLine("✓ Basic authentication test passed");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Tests that invalid credentials are properly rejected.
    /// </summary>
    [Fact]
    public async Task TestInvalidCredentialsRejected()
    {
        Console.WriteLine("\n=== Testing Invalid Credentials Rejection ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4251,
            Description = "Invalid credentials test",
            Auth = new AuthConfiguration
            {
                Username = "validuser",
                Password = "validpass"
            }
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Try to connect with wrong password
            var wrongPasswordRejected = false;
            try
            {
                var wrongOpts = new NatsOpts
                {
                    Url = "nats://127.0.0.1:4251",
                    AuthOpts = new NatsAuthOpts
                    {
                        Username = "validuser",
                        Password = "wrongpass"
                    }
                };
                await using var nats = new NatsClient(wrongOpts);
                await nats.PublishAsync("test", "test", cancellationToken: TestContext.Current.CancellationToken); // Try to use connection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✓ Connection properly rejected: {ex.Message.Substring(0, Math.Min(60, ex.Message.Length))}...");
                wrongPasswordRejected = true;
            }
            Assert.True(wrongPasswordRejected, "Connection should have been rejected with invalid credentials");

            // Try to connect with no credentials
            var noCredentialsRejected = false;
            try
            {
                var noAuthOpts = new NatsOpts
                {
                    Url = "nats://127.0.0.1:4251"
                };
                await using var nats = new NatsClient(noAuthOpts);
                await nats.PublishAsync("test", "test", cancellationToken: TestContext.Current.CancellationToken); // Try to use connection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✓ Connection properly rejected without credentials: {ex.Message.Substring(0, Math.Min(60, ex.Message.Length))}...");
                noCredentialsRejected = true;
            }
            Assert.True(noCredentialsRejected, "Connection should have been rejected without credentials");

            Console.WriteLine("✓ Invalid credentials rejection test passed");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Tests token-based authentication.
    /// </summary>
    [Fact]
    public async Task TestTokenAuthentication()
    {
        Console.WriteLine("\n=== Testing Token Authentication ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4252,
            Description = "Token authentication test",
            Auth = new AuthConfiguration
            {
                Token = "supersecrettoken123"
            }
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Try to connect with correct token using NatsOpts
            var opts = new NatsOpts
            {
                Url = "nats://127.0.0.1:4252",
                AuthOpts = new NatsAuthOpts
                {
                    Token = "supersecrettoken123"
                }
            };
            await using var nats = new NatsClient(opts);
            Console.WriteLine("✓ Connected with valid token");

            // Verify connection works with pub/sub
            var receivedMessages = new List<string>();
            var subscription = nats.SubscribeAsync<string>("test.token", cancellationToken: TestContext.Current.CancellationToken);

            var subscriptionTask = Task.Run(async () =>
            {
                await foreach (var msg in subscription)
                {
                    receivedMessages.Add(msg.Data ?? "");
                    if (receivedMessages.Count >= 2)
                        break;
                }
            }, TestContext.Current.CancellationToken);

            await Task.Delay(500, TestContext.Current.CancellationToken); // Give subscription time to establish

            await nats.PublishAsync("test.token", "Token Message 1", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("test.token", "Token Message 2", cancellationToken: TestContext.Current.CancellationToken);

            var timeoutTask = Task.Delay(5000, TestContext.Current.CancellationToken);
            var completedTask = await Task.WhenAny(subscriptionTask, timeoutTask);

            Assert.NotEqual(timeoutTask, completedTask);
            Assert.Equal(2, receivedMessages.Count);

            Console.WriteLine("✓ Published and received messages with token auth");
            Console.WriteLine("✓ Token authentication test passed");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Tests subject wildcards (* matches single token).
    /// </summary>
    [Fact]
    public async Task TestSubjectWildcards()
    {
        Console.WriteLine("\n=== Testing Subject Wildcards ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4253,
            Description = "Subject wildcards test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            await using var nats = new NatsClient("nats://127.0.0.1:4253");

            var receivedMessages = new List<string>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Subscribe with wildcard: orders.* matches orders.create, orders.update, etc.
            var subscription = nats.SubscribeAsync<string>("orders.*", cancellationToken: TestContext.Current.CancellationToken);

            _ = Task.Run(async () =>
            {
                await foreach (var msg in subscription)
                {
                    receivedMessages.Add($"{msg.Subject}:{msg.Data}");
                    if (receivedMessages.Count >= 3)
                    {
                        break;
                    }
                }
            }, TestContext.Current.CancellationToken);

            await Task.Delay(200, TestContext.Current.CancellationToken);

            // Publish to matching subjects
            await nats.PublishAsync("orders.create", "order1", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("orders.update", "order2", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("orders.delete", "order3", cancellationToken: TestContext.Current.CancellationToken);

            // Publish to non-matching subject (should not be received)
            await nats.PublishAsync("products.create", "product1", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("orders.create.nested", "nested1", cancellationToken: TestContext.Current.CancellationToken); // Should not match single-level wildcard

            await Task.Delay(1000, TestContext.Current.CancellationToken);

            Console.WriteLine($"  Received {receivedMessages.Count} messages");
            foreach (var msg in receivedMessages)
            {
                Console.WriteLine($"    {msg}");
            }

            // Should receive exactly 3 messages (orders.create, orders.update, orders.delete)
            Assert.Equal(3, receivedMessages.Count);
            Assert.Contains("orders.create:order1", receivedMessages);
            Assert.Contains("orders.update:order2", receivedMessages);
            Assert.Contains("orders.delete:order3", receivedMessages);

            Console.WriteLine("✓ Subject wildcards test passed");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Tests multi-level wildcards (> matches multiple tokens).
    /// </summary>
    [Fact]
    public async Task TestMultiLevelWildcards()
    {
        Console.WriteLine("\n=== Testing Multi-Level Wildcards ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4254,
            Description = "Multi-level wildcards test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            await using var nats = new NatsClient("nats://127.0.0.1:4254");

            var receivedMessages = new List<string>();

            // Subscribe with multi-level wildcard: orders.> matches orders.* and any deeper nesting
            var subscription = nats.SubscribeAsync<string>("orders.>", cancellationToken: TestContext.Current.CancellationToken);

            _ = Task.Run(async () =>
            {
                await foreach (var msg in subscription)
                {
                    receivedMessages.Add(msg.Subject ?? "null");
                    if (receivedMessages.Count >= 5)
                    {
                        break;
                    }
                }
            }, TestContext.Current.CancellationToken);

            await Task.Delay(200, TestContext.Current.CancellationToken);

            // All of these should match
            await nats.PublishAsync("orders.create", "msg1", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("orders.create.batch", "msg2", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("orders.update.status", "msg3", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("orders.delete.bulk.confirmed", "msg4", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("orders.query.filter.advanced", "msg5", cancellationToken: TestContext.Current.CancellationToken);

            // This should NOT match
            await nats.PublishAsync("products.create", "msg6", cancellationToken: TestContext.Current.CancellationToken);

            await Task.Delay(1000, TestContext.Current.CancellationToken);

            Console.WriteLine($"  Received {receivedMessages.Count} messages:");
            foreach (var subject in receivedMessages)
            {
                Console.WriteLine($"    {subject}");
            }

            Assert.Equal(5, receivedMessages.Count);
            Assert.Contains("orders.create", receivedMessages);
            Assert.Contains("orders.create.batch", receivedMessages);
            Assert.Contains("orders.update.status", receivedMessages);
            Assert.Contains("orders.delete.bulk.confirmed", receivedMessages);
            Assert.Contains("orders.query.filter.advanced", receivedMessages);

            Console.WriteLine("✓ Multi-level wildcards test passed");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Tests complex subject pattern matching with multiple subscribers.
    /// </summary>
    [Fact]
    public async Task TestSubjectPatternMatching()
    {
        Console.WriteLine("\n=== Testing Subject Pattern Matching ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4255,
            Description = "Subject pattern matching test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            await using var nats = new NatsClient("nats://127.0.0.1:4255");

            var specificMessages = new List<string>();
            var wildcardMessages = new List<string>();
            var allMessages = new List<string>();

            // Multiple subscribers with different patterns
            var sub1 = nats.SubscribeAsync<string>("events.user.created", cancellationToken: TestContext.Current.CancellationToken);
            var sub2 = nats.SubscribeAsync<string>("events.user.*", cancellationToken: TestContext.Current.CancellationToken);
            var sub3 = nats.SubscribeAsync<string>("events.>", cancellationToken: TestContext.Current.CancellationToken);

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub1)
                {
                    specificMessages.Add(msg.Subject ?? "null");
                }
            }, TestContext.Current.CancellationToken);

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub2)
                {
                    wildcardMessages.Add(msg.Subject ?? "null");
                }
            }, TestContext.Current.CancellationToken);

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub3)
                {
                    allMessages.Add(msg.Subject ?? "null");
                    if (allMessages.Count >= 4)
                    {
                        break;
                    }
                }
            }, TestContext.Current.CancellationToken);

            await Task.Delay(200, TestContext.Current.CancellationToken);

            // Publish various events
            await nats.PublishAsync("events.user.created", "user1", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("events.user.updated", "user2", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("events.user.deleted", "user3", cancellationToken: TestContext.Current.CancellationToken);
            await nats.PublishAsync("events.order.created", "order1", cancellationToken: TestContext.Current.CancellationToken);

            await Task.Delay(1000, TestContext.Current.CancellationToken);

            Console.WriteLine($"  Specific subscriber received: {specificMessages.Count} messages");
            Console.WriteLine($"  Wildcard subscriber received: {wildcardMessages.Count} messages");
            Console.WriteLine($"  All-events subscriber received: {allMessages.Count} messages");

            // Specific subscriber should receive only 1 message
            Assert.Single(specificMessages);
            Assert.Contains("events.user.created", specificMessages);

            // Wildcard subscriber should receive 3 messages (all events.user.*)
            Assert.Equal(3, wildcardMessages.Count);

            // All-events subscriber should receive all 4 messages
            Assert.Equal(4, allMessages.Count);

            Console.WriteLine("✓ Subject pattern matching test passed");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Tests request-reply pattern with subject filtering.
    /// </summary>
    [Fact]
    public async Task TestRequestReplyPattern()
    {
        Console.WriteLine("\n=== Testing Request-Reply Pattern ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4256,
            Description = "Request-reply pattern test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            // Create two connections: one for the service, one for the client
            await using var serviceConn = new NatsClient("nats://127.0.0.1:4256");
            await using var clientConn = new NatsClient("nats://127.0.0.1:4256");

            // Set up a service that responds to requests
            var subscription = serviceConn.SubscribeAsync<string>("service.echo", cancellationToken: TestContext.Current.CancellationToken);

            _ = Task.Run(async () =>
            {
                await foreach (var msg in subscription)
                {
                    if (msg.ReplyTo != null)
                    {
                        var response = $"Echo: {msg.Data}";
                        await serviceConn.PublishAsync(msg.ReplyTo, response);
                        Console.WriteLine($"  Service replied to {msg.ReplyTo} with: {response}");
                    }
                }
            }, TestContext.Current.CancellationToken);

            await Task.Delay(200, TestContext.Current.CancellationToken);

            // Client sends request and waits for reply
            Console.WriteLine("  Client sending request...");
            var reply = await clientConn.RequestAsync<string, string>("service.echo", "Hello, Service!"
, cancellationToken: TestContext.Current.CancellationToken);

            Console.WriteLine($"  Client received reply: {reply.Data}");

            Assert.Equal("Echo: Hello, Service!", reply.Data);

            Console.WriteLine("✓ Request-reply pattern test passed");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Tests queue groups for load balancing subscribers.
    /// </summary>
    [Fact]
    public async Task TestQueueGroups()
    {
        Console.WriteLine("\n=== Testing Queue Groups ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4257,
            Description = "Queue groups test"
        };

        var result = await controller.ConfigureAsync(config, TestContext.Current.CancellationToken);
        Assert.True(result.Success, $"Failed to start server: {result.ErrorMessage}");

        await Task.Delay(500, TestContext.Current.CancellationToken);

        try
        {
            await using var nats = new NatsClient("nats://127.0.0.1:4257");

            var worker1Messages = new List<string>();
            var worker2Messages = new List<string>();
            var worker3Messages = new List<string>();

            // Create 3 workers in the same queue group "workers"
            var sub1 = nats.SubscribeAsync<string>("tasks.process", queueGroup: "workers", cancellationToken: TestContext.Current.CancellationToken);
            var sub2 = nats.SubscribeAsync<string>("tasks.process", queueGroup: "workers", cancellationToken: TestContext.Current.CancellationToken);
            var sub3 = nats.SubscribeAsync<string>("tasks.process", queueGroup: "workers", cancellationToken: TestContext.Current.CancellationToken);

            var totalReceived = 0;
            var totalLock = new object();

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub1)
                {
                    worker1Messages.Add(msg.Data ?? "null");
                    lock (totalLock) { totalReceived++; }
                }
            }, TestContext.Current.CancellationToken);

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub2)
                {
                    worker2Messages.Add(msg.Data ?? "null");
                    lock (totalLock) { totalReceived++; }
                }
            }, TestContext.Current.CancellationToken);

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub3)
                {
                    worker3Messages.Add(msg.Data ?? "null");
                    lock (totalLock) { totalReceived++; }
                }
            }, TestContext.Current.CancellationToken);

            await Task.Delay(200, TestContext.Current.CancellationToken);

            // Publish 12 messages
            Console.WriteLine("  Publishing 12 tasks...");
            for (int i = 1; i <= 12; i++)
            {
                await nats.PublishAsync("tasks.process", $"task{i}", cancellationToken: TestContext.Current.CancellationToken);
                await Task.Delay(50, TestContext.Current.CancellationToken); // Small delay to ensure messages are distributed
            }

            // Wait for messages to be processed
            await Task.Delay(1000, TestContext.Current.CancellationToken);

            Console.WriteLine($"  Worker 1 received: {worker1Messages.Count} messages");
            Console.WriteLine($"  Worker 2 received: {worker2Messages.Count} messages");
            Console.WriteLine($"  Worker 3 received: {worker3Messages.Count} messages");
            Console.WriteLine($"  Total received: {totalReceived} messages");

            // Each message should be received by exactly one worker
            Assert.Equal(12, totalReceived);

            // Messages should be distributed (not all to one worker)
            Assert.True(worker1Messages.Count < 12, "Messages not distributed across queue group members");
            Assert.True(worker2Messages.Count < 12, "Messages not distributed across queue group members");
            Assert.True(worker3Messages.Count < 12, "Messages not distributed across queue group members");

            // Each worker should have received at least one message (with 12 messages and 3 workers)
            if (worker1Messages.Count == 0 || worker2Messages.Count == 0 || worker3Messages.Count == 0)
            {
                Console.WriteLine("⚠ Warning: Not all workers received messages (may be timing issue)");
                // Don't fail the test for this, as distribution can be uneven
            }

            Console.WriteLine("✓ Queue groups test passed");
        }
        finally
        {
            await controller.ShutdownAsync(TestContext.Current.CancellationToken);
        }
    }
}
