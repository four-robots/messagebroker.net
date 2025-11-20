using System.Text;
using System.Text.Json;
using DotGnatly.Core.Configuration;
using DotGnatly.Nats.Implementation;
using NATS.Net;
using NATS.Client.Core;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Integration test suite wrapper for account authorization and subject filtering tests.
/// </summary>
public class AuthorizationAndFilteringTestSuite : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        await results.AssertAsync("Basic Authentication", AuthorizationAndFilteringTests.TestBasicAuthentication);
        await results.AssertAsync("Invalid Credentials Rejected", AuthorizationAndFilteringTests.TestInvalidCredentialsRejected);
        await results.AssertAsync("Token Authentication", AuthorizationAndFilteringTests.TestTokenAuthentication);
        await results.AssertAsync("Subject Wildcards", AuthorizationAndFilteringTests.TestSubjectWildcards);
        await results.AssertAsync("Subject Pattern Matching", AuthorizationAndFilteringTests.TestSubjectPatternMatching);
        await results.AssertAsync("Request-Reply Pattern", AuthorizationAndFilteringTests.TestRequestReplyPattern);
        await results.AssertAsync("Multi-Level Wildcards", AuthorizationAndFilteringTests.TestMultiLevelWildcards);
        await results.AssertAsync("Queue Groups", AuthorizationAndFilteringTests.TestQueueGroups);
    }
}

/// <summary>
/// Integration tests for NATS server account authorization and subject filtering.
/// Tests client connectivity using NATS.Net package.
/// </summary>
public static class AuthorizationAndFilteringTests
{
    /// <summary>
    /// Tests basic username/password authentication.
    /// </summary>
    public static async Task<bool> TestBasicAuthentication()
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

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

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
            var subscription = nats.SubscribeAsync<string>("test.auth");

            var subscriptionTask = Task.Run(async () =>
            {
                await foreach (var msg in subscription)
                {
                    receivedMessages.Add(msg.Data ?? "");
                    if (receivedMessages.Count >= 3)
                        break;
                }
            });

            await Task.Delay(500); // Give subscription time to establish

            await nats.PublishAsync("test.auth", "Message 1");
            await nats.PublishAsync("test.auth", "Message 2");
            await nats.PublishAsync("test.auth", "Message 3");

            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(subscriptionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Console.WriteLine("❌ Timeout waiting for messages");
                return false;
            }

            if (receivedMessages.Count != 3)
            {
                Console.WriteLine($"❌ Expected 3 messages, received {receivedMessages.Count}");
                return false;
            }

            Console.WriteLine("✓ Successfully published and received 3 messages");
            Console.WriteLine("✓ Basic authentication test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Basic authentication test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    /// <summary>
    /// Tests that invalid credentials are properly rejected.
    /// </summary>
    public static async Task<bool> TestInvalidCredentialsRejected()
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

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Try to connect with wrong password
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
                await nats.PublishAsync("test", "test"); // Try to use connection
                Console.WriteLine("❌ Connection should have been rejected with invalid credentials");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✓ Connection properly rejected: {ex.Message.Substring(0, Math.Min(60, ex.Message.Length))}...");
            }

            // Try to connect with no credentials
            try
            {
                var noAuthOpts = new NatsOpts
                {
                    Url = "nats://127.0.0.1:4251"
                };
                await using var nats = new NatsClient(noAuthOpts);
                await nats.PublishAsync("test", "test"); // Try to use connection
                Console.WriteLine("❌ Connection should have been rejected without credentials");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✓ Connection properly rejected without credentials: {ex.Message.Substring(0, Math.Min(60, ex.Message.Length))}...");
            }

            Console.WriteLine("✓ Invalid credentials rejection test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Invalid credentials test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    /// <summary>
    /// Tests token-based authentication.
    /// </summary>
    public static async Task<bool> TestTokenAuthentication()
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

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

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
            var subscription = nats.SubscribeAsync<string>("test.token");

            var subscriptionTask = Task.Run(async () =>
            {
                await foreach (var msg in subscription)
                {
                    receivedMessages.Add(msg.Data ?? "");
                    if (receivedMessages.Count >= 2)
                        break;
                }
            });

            await Task.Delay(500); // Give subscription time to establish

            await nats.PublishAsync("test.token", "Token Message 1");
            await nats.PublishAsync("test.token", "Token Message 2");

            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(subscriptionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Console.WriteLine("❌ Timeout waiting for messages");
                return false;
            }

            if (receivedMessages.Count != 2)
            {
                Console.WriteLine($"❌ Expected 2 messages, received {receivedMessages.Count}");
                return false;
            }

            Console.WriteLine("✓ Published and received messages with token auth");
            Console.WriteLine("✓ Token authentication test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Token authentication test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    /// <summary>
    /// Tests subject wildcards (* matches single token).
    /// </summary>
    public static async Task<bool> TestSubjectWildcards()
    {
        Console.WriteLine("\n=== Testing Subject Wildcards ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4253,
            Description = "Subject wildcards test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            await using var nats = new NatsClient("nats://127.0.0.1:4253");

            var receivedMessages = new List<string>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Subscribe with wildcard: orders.* matches orders.create, orders.update, etc.
            var subscription = nats.SubscribeAsync<string>("orders.*");

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
            });

            await Task.Delay(200);

            // Publish to matching subjects
            await nats.PublishAsync("orders.create", "order1");
            await nats.PublishAsync("orders.update", "order2");
            await nats.PublishAsync("orders.delete", "order3");

            // Publish to non-matching subject (should not be received)
            await nats.PublishAsync("products.create", "product1");
            await nats.PublishAsync("orders.create.nested", "nested1"); // Should not match single-level wildcard

            await Task.Delay(1000);

            Console.WriteLine($"  Received {receivedMessages.Count} messages");
            foreach (var msg in receivedMessages)
            {
                Console.WriteLine($"    {msg}");
            }

            // Should receive exactly 3 messages (orders.create, orders.update, orders.delete)
            if (receivedMessages.Count != 3)
            {
                Console.WriteLine($"❌ Expected 3 messages, got {receivedMessages.Count}");
                return false;
            }

            if (!receivedMessages.Contains("orders.create:order1") ||
                !receivedMessages.Contains("orders.update:order2") ||
                !receivedMessages.Contains("orders.delete:order3"))
            {
                Console.WriteLine("❌ Did not receive expected messages");
                return false;
            }

            Console.WriteLine("✓ Subject wildcards test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Subject wildcards test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    /// <summary>
    /// Tests multi-level wildcards (> matches multiple tokens).
    /// </summary>
    public static async Task<bool> TestMultiLevelWildcards()
    {
        Console.WriteLine("\n=== Testing Multi-Level Wildcards ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4254,
            Description = "Multi-level wildcards test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            await using var nats = new NatsClient("nats://127.0.0.1:4254");

            var receivedMessages = new List<string>();

            // Subscribe with multi-level wildcard: orders.> matches orders.* and any deeper nesting
            var subscription = nats.SubscribeAsync<string>("orders.>");

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
            });

            await Task.Delay(200);

            // All of these should match
            await nats.PublishAsync("orders.create", "msg1");
            await nats.PublishAsync("orders.create.batch", "msg2");
            await nats.PublishAsync("orders.update.status", "msg3");
            await nats.PublishAsync("orders.delete.bulk.confirmed", "msg4");
            await nats.PublishAsync("orders.query.filter.advanced", "msg5");

            // This should NOT match
            await nats.PublishAsync("products.create", "msg6");

            await Task.Delay(1000);

            Console.WriteLine($"  Received {receivedMessages.Count} messages:");
            foreach (var subject in receivedMessages)
            {
                Console.WriteLine($"    {subject}");
            }

            if (receivedMessages.Count != 5)
            {
                Console.WriteLine($"❌ Expected 5 messages, got {receivedMessages.Count}");
                return false;
            }

            if (!receivedMessages.Contains("orders.create") ||
                !receivedMessages.Contains("orders.create.batch") ||
                !receivedMessages.Contains("orders.update.status") ||
                !receivedMessages.Contains("orders.delete.bulk.confirmed") ||
                !receivedMessages.Contains("orders.query.filter.advanced"))
            {
                Console.WriteLine("❌ Did not receive expected messages");
                return false;
            }

            Console.WriteLine("✓ Multi-level wildcards test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Multi-level wildcards test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    /// <summary>
    /// Tests complex subject pattern matching with multiple subscribers.
    /// </summary>
    public static async Task<bool> TestSubjectPatternMatching()
    {
        Console.WriteLine("\n=== Testing Subject Pattern Matching ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4255,
            Description = "Subject pattern matching test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            await using var nats = new NatsClient("nats://127.0.0.1:4255");

            var specificMessages = new List<string>();
            var wildcardMessages = new List<string>();
            var allMessages = new List<string>();

            // Multiple subscribers with different patterns
            var sub1 = nats.SubscribeAsync<string>("events.user.created");
            var sub2 = nats.SubscribeAsync<string>("events.user.*");
            var sub3 = nats.SubscribeAsync<string>("events.>");

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub1)
                {
                    specificMessages.Add(msg.Subject ?? "null");
                }
            });

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub2)
                {
                    wildcardMessages.Add(msg.Subject ?? "null");
                }
            });

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
            });

            await Task.Delay(200);

            // Publish various events
            await nats.PublishAsync("events.user.created", "user1");
            await nats.PublishAsync("events.user.updated", "user2");
            await nats.PublishAsync("events.user.deleted", "user3");
            await nats.PublishAsync("events.order.created", "order1");

            await Task.Delay(1000);

            Console.WriteLine($"  Specific subscriber received: {specificMessages.Count} messages");
            Console.WriteLine($"  Wildcard subscriber received: {wildcardMessages.Count} messages");
            Console.WriteLine($"  All-events subscriber received: {allMessages.Count} messages");

            // Specific subscriber should receive only 1 message
            if (specificMessages.Count != 1 || !specificMessages.Contains("events.user.created"))
            {
                Console.WriteLine($"❌ Specific subscriber expected 1 message, got {specificMessages.Count}");
                return false;
            }

            // Wildcard subscriber should receive 3 messages (all events.user.*)
            if (wildcardMessages.Count != 3)
            {
                Console.WriteLine($"❌ Wildcard subscriber expected 3 messages, got {wildcardMessages.Count}");
                return false;
            }

            // All-events subscriber should receive all 4 messages
            if (allMessages.Count != 4)
            {
                Console.WriteLine($"❌ All-events subscriber expected 4 messages, got {allMessages.Count}");
                return false;
            }

            Console.WriteLine("✓ Subject pattern matching test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Subject pattern matching test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    /// <summary>
    /// Tests request-reply pattern with subject filtering.
    /// </summary>
    public static async Task<bool> TestRequestReplyPattern()
    {
        Console.WriteLine("\n=== Testing Request-Reply Pattern ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4256,
            Description = "Request-reply pattern test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            // Create two connections: one for the service, one for the client
            await using var serviceConn = new NatsClient("nats://127.0.0.1:4256");
            await using var clientConn = new NatsClient("nats://127.0.0.1:4256");

            // Set up a service that responds to requests
            var subscription = serviceConn.SubscribeAsync<string>("service.echo");

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
            });

            await Task.Delay(200);

            // Client sends request and waits for reply
            Console.WriteLine("  Client sending request...");
            var reply = await clientConn.RequestAsync<string, string>(
                "service.echo",
                "Hello, Service!"
            );

            Console.WriteLine($"  Client received reply: {reply.Data}");

            if (reply.Data != "Echo: Hello, Service!")
            {
                Console.WriteLine($"❌ Expected 'Echo: Hello, Service!', got '{reply.Data}'");
                return false;
            }

            Console.WriteLine("✓ Request-reply pattern test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Request-reply pattern test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }

    /// <summary>
    /// Tests queue groups for load balancing subscribers.
    /// </summary>
    public static async Task<bool> TestQueueGroups()
    {
        Console.WriteLine("\n=== Testing Queue Groups ===");

        using var controller = new NatsController();

        var config = new BrokerConfiguration
        {
            Host = "127.0.0.1",
            Port = 4257,
            Description = "Queue groups test"
        };

        var result = await controller.ConfigureAsync(config);
        if (!result.Success)
        {
            Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
            return false;
        }

        await Task.Delay(500);

        try
        {
            await using var nats = new NatsClient("nats://127.0.0.1:4257");

            var worker1Messages = new List<string>();
            var worker2Messages = new List<string>();
            var worker3Messages = new List<string>();

            // Create 3 workers in the same queue group "workers"
            var sub1 = nats.SubscribeAsync<string>("tasks.process", queueGroup: "workers");
            var sub2 = nats.SubscribeAsync<string>("tasks.process", queueGroup: "workers");
            var sub3 = nats.SubscribeAsync<string>("tasks.process", queueGroup: "workers");

            var totalReceived = 0;
            var totalLock = new object();

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub1)
                {
                    worker1Messages.Add(msg.Data ?? "null");
                    lock (totalLock) { totalReceived++; }
                }
            });

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub2)
                {
                    worker2Messages.Add(msg.Data ?? "null");
                    lock (totalLock) { totalReceived++; }
                }
            });

            _ = Task.Run(async () =>
            {
                await foreach (var msg in sub3)
                {
                    worker3Messages.Add(msg.Data ?? "null");
                    lock (totalLock) { totalReceived++; }
                }
            });

            await Task.Delay(200);

            // Publish 12 messages
            Console.WriteLine("  Publishing 12 tasks...");
            for (int i = 1; i <= 12; i++)
            {
                await nats.PublishAsync("tasks.process", $"task{i}");
                await Task.Delay(50); // Small delay to ensure messages are distributed
            }

            // Wait for messages to be processed
            await Task.Delay(1000);

            Console.WriteLine($"  Worker 1 received: {worker1Messages.Count} messages");
            Console.WriteLine($"  Worker 2 received: {worker2Messages.Count} messages");
            Console.WriteLine($"  Worker 3 received: {worker3Messages.Count} messages");
            Console.WriteLine($"  Total received: {totalReceived} messages");

            // Each message should be received by exactly one worker
            if (totalReceived != 12)
            {
                Console.WriteLine($"❌ Expected total of 12 messages, got {totalReceived}");
                return false;
            }

            // Messages should be distributed (not all to one worker)
            if (worker1Messages.Count == 12 || worker2Messages.Count == 12 || worker3Messages.Count == 12)
            {
                Console.WriteLine("❌ Messages not distributed across queue group members");
                return false;
            }

            // Each worker should have received at least one message (with 12 messages and 3 workers)
            if (worker1Messages.Count == 0 || worker2Messages.Count == 0 || worker3Messages.Count == 0)
            {
                Console.WriteLine("⚠ Warning: Not all workers received messages (may be timing issue)");
                // Don't fail the test for this, as distribution can be uneven
            }

            Console.WriteLine("✓ Queue groups test passed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Queue groups test failed: {ex.Message}");
            return false;
        }
        finally
        {
            await controller.ShutdownAsync();
        }
    }
}
