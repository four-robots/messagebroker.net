using MessageBroker.Core.Configuration;
using MessageBroker.Core.Validation;
using MessageBroker.Nats.Implementation;

namespace MessageBroker.IntegrationTests;

/// <summary>
/// Tests configuration validation and error handling.
/// </summary>
public class ValidationTests : IIntegrationTest
{
    public async Task RunAsync(TestResults results)
    {
        // Test 1: Invalid port validation
        await results.AssertAsync(
            "Validation rejects invalid port numbers",
            async () =>
            {
                using var server = new NatsController();
                var result = await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 99999 // Invalid port
                });

                return !result.Success && result.ValidationResult.HasErrors;
            });

        // Test 2: Invalid subject pattern validation
        await results.AssertAsync(
            "Validation rejects invalid subject patterns",
            async () =>
            {
                using var server = new NatsController();
                var result = await server.ConfigureAsync(new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string> { ".invalid", "also..bad", "bad." }
                    }
                });

                return !result.Success && result.ValidationResult.HasErrors;
            });

        // Test 3: Empty subject validation
        await results.AssertAsync(
            "Validation rejects empty subjects",
            async () =>
            {
                var validator = new ConfigurationValidator();
                var config = new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string> { "" }
                    }
                };

                var result = validator.Validate(config);
                return result.HasErrors;
            });

        // Test 4: Valid subject patterns
        await results.AssertAsync(
            "Validation accepts valid subject patterns",
            async () =>
            {
                var validator = new ConfigurationValidator();
                var config = new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string>
                        {
                            "events.>",
                            "data.*",
                            "logs.*.error",
                            ">",
                            "simple",
                            "with-dashes",
                            "with_underscores"
                        }
                    }
                };

                var result = validator.Validate(config);
                return !result.HasErrors;
            });

        // Test 5: Port conflict validation
        await results.AssertAsync(
            "Validation rejects port conflicts (main port = leaf port)",
            async () =>
            {
                var validator = new ConfigurationValidator();
                var config = new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 4222 // Same as main port
                    }
                };

                var result = validator.Validate(config);
                return result.HasErrors;
            });

        // Test 6: JetStream validation
        await results.AssertAsync(
            "Validation enforces JetStream requirements",
            async () =>
            {
                var validator = new ConfigurationValidator();
                var config = new BrokerConfiguration
                {
                    Port = 4222,
                    Jetstream = true,
                    JetstreamStoreDir = "" // Empty store dir
                };

                var result = validator.Validate(config);
                return result.HasErrors;
            });

        // Test 7: Authentication validation
        await results.AssertAsync(
            "Validation rejects mismatched auth credentials",
            async () =>
            {
                var validator = new ConfigurationValidator();
                var config = new BrokerConfiguration
                {
                    Port = 4222,
                    Auth = new AuthConfiguration
                    {
                        Username = "user",
                        Password = null // Username without password
                    }
                };

                var result = validator.Validate(config);
                return result.HasErrors;
            });

        // Test 8: Leaf node auth validation
        await results.AssertAsync(
            "Validation rejects mismatched leaf node auth",
            async () =>
            {
                var validator = new ConfigurationValidator();
                var config = new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        AuthUsername = "user",
                        AuthPassword = null // Username without password
                    }
                };

                var result = validator.Validate(config);
                return result.HasErrors;
            });

        // Test 9: Validation prevents invalid hot reload
        await results.AssertAsync(
            "Hot reload validation prevents invalid changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                var result = await server.ApplyChangesAsync(c => c.Port = 99999);

                return !result.Success && result.ValidationResult.HasErrors;
            });

        // Test 10: Validation accepts valid hot reload
        await results.AssertAsync(
            "Hot reload validation accepts valid changes",
            async () =>
            {
                using var server = new NatsController();
                await server.ConfigureAsync(new BrokerConfiguration { Port = 4222 });

                var result = await server.ApplyChangesAsync(c => c.Debug = true);

                await server.ShutdownAsync();
                return result.Success;
            });

        // Test 11: Multiple validation errors
        await results.AssertAsync(
            "Validation reports multiple errors",
            async () =>
            {
                var validator = new ConfigurationValidator();
                var config = new BrokerConfiguration
                {
                    Port = 99999,          // Error 1
                    MaxPayload = -1,       // Error 2
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 99999,      // Error 3
                        ImportSubjects = new List<string> { ".bad" } // Error 4
                    }
                };

                var result = validator.Validate(config);
                return result.Errors.Count >= 3; // At least 3 errors
            });

        // Test 12: Invalid wildcard placement
        await results.AssertAsync(
            "Validation rejects invalid wildcard placement",
            async () =>
            {
                var validator = new ConfigurationValidator();
                var config = new BrokerConfiguration
                {
                    Port = 4222,
                    LeafNode = new LeafNodeConfiguration
                    {
                        Port = 7422,
                        ImportSubjects = new List<string>
                        {
                            "bad.>.middle",  // > must be at the end
                            "bad>wildcard"   // > must be preceded by dot
                        }
                    }
                };

                var result = validator.Validate(config);
                return result.HasErrors;
            });
    }
}
