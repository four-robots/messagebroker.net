using DotGnatly.Core.Configuration;
using DotGnatly.Core.Validation;
using DotGnatly.Nats.Implementation;
using Xunit;

namespace DotGnatly.IntegrationTests;

/// <summary>
/// Tests configuration validation and error handling.
/// </summary>
public class ValidationTests
{
    [Fact]
    public async Task ValidationRejectsInvalidPortNumbers()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 99999 // Invalid port
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidationRejectsInvalidSubjectPatterns()
    {
        using var server = new NatsController();
        var result = await server.ConfigureAsync(new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string> { ".invalid", "also..bad", "bad." }
            }
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ValidationRejectsEmptySubjects()
    {
        var validator = new ConfigurationValidator();
        var config = new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string> { "" }
            }
        };

        var result = validator.Validate(config);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidationAcceptsValidSubjectPatterns()
    {
        var validator = new ConfigurationValidator();
        var config = new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
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
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidationRejectsPortConflicts()
    {
        var validator = new ConfigurationValidator();
        var config = new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 14222 // Same as main port
            }
        };

        var result = validator.Validate(config);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidationEnforcesJetStreamRequirements()
    {
        var validator = new ConfigurationValidator();
        var config = new BrokerConfiguration
        {
            Port = 14222,
            Jetstream = true,
            JetstreamStoreDir = "" // Empty store dir
        };

        var result = validator.Validate(config);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidationRejectsMismatchedAuthCredentials()
    {
        var validator = new ConfigurationValidator();
        var config = new BrokerConfiguration
        {
            Port = 14222,
            Auth = new AuthConfiguration
            {
                Username = "user",
                Password = null // Username without password
            }
        };

        var result = validator.Validate(config);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidationRejectsMismatchedLeafNodeAuth()
    {
        var validator = new ConfigurationValidator();
        var config = new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                AuthUsername = "user",
                AuthPassword = null // Username without password
            }
        };

        var result = validator.Validate(config);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task HotReloadValidationPreventsInvalidChanges()
    {
        using var server = new NatsController();
        await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);

        var result = await server.ApplyChangesAsync(c => c.Port = 99999, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task HotReloadValidationAcceptsValidChanges()
    {
        using var server = new NatsController();
        await server.ConfigureAsync(new BrokerConfiguration { Port = 14222 }, TestContext.Current.CancellationToken);

        var result = await server.ApplyChangesAsync(c => c.Debug = true, TestContext.Current.CancellationToken);

        await server.ShutdownAsync(TestContext.Current.CancellationToken);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ValidationReportsMultipleErrors()
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
        Assert.True(result.Errors.Count >= 3); // At least 3 errors
    }

    [Fact]
    public async Task ValidationRejectsInvalidWildcardPlacement()
    {
        var validator = new ConfigurationValidator();
        var config = new BrokerConfiguration
        {
            Port = 14222,
            LeafNode = new LeafNodeConfiguration
            {
                Port = 17422,
                ImportSubjects = new List<string>
                {
                    "bad.>.middle",  // > must be at the end
                    "bad>wildcard"   // > must be preceded by dot
                }
            }
        };

        var result = validator.Validate(config);
        Assert.False(result.IsValid);
    }
}
