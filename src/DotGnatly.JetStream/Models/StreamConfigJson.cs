using System.Text.Json;
using System.Text.Json.Serialization;
using NATS.Client.JetStream.Models;

namespace DotGnatly.JetStream.Models;

/// <summary>
/// JSON model for deserializing NATS JetStream stream configuration files.
/// This matches the JSON format used by NATS CLI and configuration files.
/// </summary>
public class StreamConfigJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subjects")]
    public List<string> Subjects { get; set; } = new();

    [JsonPropertyName("retention")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RetentionJson Retention { get; set; } = RetentionJson.Limits;

    [JsonPropertyName("max_consumers")]
    public int MaxConsumers { get; set; } = -1;

    [JsonPropertyName("max_msgs")]
    public long MaxMsgs { get; set; } = -1;

    [JsonPropertyName("max_bytes")]
    public long MaxBytes { get; set; } = -1;

    [JsonPropertyName("max_age")]
    public long MaxAge { get; set; } = 0; // nanoseconds

    [JsonPropertyName("max_msg_size")]
    public int MaxMsgSize { get; set; } = -1;

    [JsonPropertyName("max_msgs_per_subject")]
    public long MaxMsgsPerSubject { get; set; } = -1;

    [JsonPropertyName("storage")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StorageJson Storage { get; set; } = StorageJson.File;

    [JsonPropertyName("discard")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DiscardJson Discard { get; set; } = DiscardJson.Old;

    [JsonPropertyName("num_replicas")]
    public int NumReplicas { get; set; } = 1;

    [JsonPropertyName("duplicate_window")]
    public long DuplicateWindow { get; set; } = 0; // nanoseconds

    [JsonPropertyName("placement")]
    public PlacementJson? Placement { get; set; }

    [JsonPropertyName("sources")]
    public List<StreamSourceJson>? Sources { get; set; }

    [JsonPropertyName("sealed")]
    public bool Sealed { get; set; } = false;

    [JsonPropertyName("deny_delete")]
    public bool DenyDelete { get; set; } = false;

    [JsonPropertyName("deny_purge")]
    public bool DenyPurge { get; set; } = false;

    [JsonPropertyName("allow_rollup_hdrs")]
    public bool AllowRollupHdrs { get; set; } = false;

    [JsonPropertyName("allow_direct")]
    public bool AllowDirect { get; set; } = false;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Converts this JSON model to a StreamConfigBuilder.
    /// </summary>
    /// <returns>A configured StreamConfigBuilder instance.</returns>
    public StreamConfigBuilder ToBuilder()
    {
        var builder = new StreamConfigBuilder(Name);

        // Basic configuration
        if (Subjects.Count > 0)
        {
            builder.WithSubjects(Subjects.ToArray());
        }

        builder
            .WithRetention(ConvertRetention(Retention))
            .WithStorage(ConvertStorage(Storage))
            .WithDiscard(ConvertDiscard(Discard))
            .WithReplicas(NumReplicas);

        // Limits
        if (MaxMsgs != -1) builder.WithMaxMessages(MaxMsgs);
        if (MaxBytes != -1) builder.WithMaxBytes(MaxBytes);
        if (MaxAge > 0) builder.WithMaxAge(TimeSpan.FromTicks(MaxAge / 100)); // Convert nanoseconds to ticks
        if (MaxMsgSize != -1) builder.WithMaxMessageSize(MaxMsgSize);
        if (MaxConsumers != -1) builder.WithMaxConsumers(MaxConsumers);
        if (MaxMsgsPerSubject != -1) builder.WithMaxMessagesPerSubject(MaxMsgsPerSubject);

        // Duplicate window
        if (DuplicateWindow > 0)
        {
            builder.WithDuplicateWindow(TimeSpan.FromTicks(DuplicateWindow / 100)); // Convert nanoseconds to ticks
        }

        // Description
        if (!string.IsNullOrWhiteSpace(Description))
        {
            builder.WithDescription(Description);
        }

        // Placement
        if (Placement != null)
        {
            builder.WithPlacement(Placement.Cluster, Placement.Tags?.ToArray());
        }

        // Sources
        if (Sources != null && Sources.Count > 0)
        {
            foreach (var source in Sources)
            {
                builder.AddSource(
                    source.Name,
                    source.FilterSubject,
                    source.External?.Api,
                    source.External?.Deliver
                );
            }
        }

        // Advanced flags
        if (Sealed) builder.WithSealed();
        if (DenyDelete) builder.WithDenyDelete();
        if (DenyPurge) builder.WithDenyPurge();
        if (AllowRollupHdrs) builder.WithAllowRollup();
        if (AllowDirect) builder.WithAllowDirect();

        return builder;
    }

    /// <summary>
    /// Loads a stream configuration from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A StreamConfigJson instance.</returns>
    public static StreamConfigJson FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        return JsonSerializer.Deserialize<StreamConfigJson>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize stream configuration");
    }

    /// <summary>
    /// Loads a stream configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>A StreamConfigJson instance.</returns>
    public static async Task<StreamConfigJson> FromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Stream configuration file not found: {filePath}", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);
        return FromJson(json);
    }

    /// <summary>
    /// Loads multiple stream configurations from a directory.
    /// </summary>
    /// <param name="directoryPath">The directory containing JSON configuration files.</param>
    /// <param name="pattern">The file pattern to match (default: *.json).</param>
    /// <returns>A list of StreamConfigJson instances.</returns>
    public static async Task<List<StreamConfigJson>> FromDirectoryAsync(string directoryPath, string pattern = "*.json")
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Stream configuration directory not found: {directoryPath}");
        }

        var configs = new List<StreamConfigJson>();
        var files = Directory.GetFiles(directoryPath, pattern);

        foreach (var file in files)
        {
            try
            {
                var config = await FromFileAsync(file);
                configs.Add(config);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load stream configuration from {file}: {ex.Message}", ex);
            }
        }

        return configs;
    }

    private static StreamConfigRetention ConvertRetention(RetentionJson retention) => retention switch
    {
        RetentionJson.Limits => StreamConfigRetention.Limits,
        RetentionJson.Interest => StreamConfigRetention.Interest,
        RetentionJson.Workqueue => StreamConfigRetention.Workqueue,
        _ => StreamConfigRetention.Limits
    };

    private static StreamConfigStorage ConvertStorage(StorageJson storage) => storage switch
    {
        StorageJson.File => StreamConfigStorage.File,
        StorageJson.Memory => StreamConfigStorage.Memory,
        _ => StreamConfigStorage.File
    };

    private static StreamConfigDiscard ConvertDiscard(DiscardJson discard) => discard switch
    {
        DiscardJson.Old => StreamConfigDiscard.Old,
        DiscardJson.New => StreamConfigDiscard.New,
        _ => StreamConfigDiscard.Old
    };
}

/// <summary>
/// JSON model for placement configuration.
/// </summary>
public class PlacementJson
{
    [JsonPropertyName("cluster")]
    public string? Cluster { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

/// <summary>
/// JSON model for stream source configuration.
/// </summary>
public class StreamSourceJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("filter_subject")]
    public string? FilterSubject { get; set; }

    [JsonPropertyName("external")]
    public ExternalJson? External { get; set; }
}

/// <summary>
/// JSON model for external source configuration (hub-and-spoke).
/// </summary>
public class ExternalJson
{
    [JsonPropertyName("api")]
    public string? Api { get; set; }

    [JsonPropertyName("deliver")]
    public string? Deliver { get; set; }
}

/// <summary>
/// JSON enum for retention policy.
/// </summary>
public enum RetentionJson
{
    [JsonPropertyName("limits")]
    Limits,

    [JsonPropertyName("interest")]
    Interest,

    [JsonPropertyName("workqueue")]
    Workqueue
}

/// <summary>
/// JSON enum for storage type.
/// </summary>
public enum StorageJson
{
    [JsonPropertyName("file")]
    File,

    [JsonPropertyName("memory")]
    Memory
}

/// <summary>
/// JSON enum for discard policy.
/// </summary>
public enum DiscardJson
{
    [JsonPropertyName("old")]
    Old,

    [JsonPropertyName("new")]
    New
}
