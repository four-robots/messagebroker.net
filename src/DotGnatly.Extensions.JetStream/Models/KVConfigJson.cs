using System.Text.Json;
using System.Text.Json.Serialization;
using NATS.Client.KeyValueStore;

namespace DotGnatly.Extensions.JetStream.Models;

/// <summary>
/// JSON model for deserializing NATS Key-Value store configuration files.
/// </summary>
public class KVConfigJson
{
    [JsonPropertyName("bucket")]
    public string Bucket { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("max_history_per_key")]
    public long MaxHistoryPerKey { get; set; } = 1;

    [JsonPropertyName("ttl")]
    public long TTL { get; set; } = 0; // nanoseconds

    [JsonPropertyName("max_bucket_size")]
    public long MaxBucketSize { get; set; } = -1;

    [JsonPropertyName("replicas")]
    public int Replicas { get; set; } = 1;

    [JsonPropertyName("storage")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public KVStorageTypeJson Storage { get; set; } = KVStorageTypeJson.File;

    /// <summary>
    /// Converts this JSON model to a KVConfigBuilder.
    /// </summary>
    /// <returns>A configured KVConfigBuilder instance.</returns>
    public KVConfigBuilder ToBuilder()
    {
        var builder = new KVConfigBuilder(Bucket);

        if (!string.IsNullOrWhiteSpace(Description))
        {
            builder.WithDescription(Description);
        }

        builder.WithMaxHistoryPerKey(MaxHistoryPerKey);

        if (TTL > 0)
        {
            builder.WithTTL(TimeSpan.FromTicks(TTL / 100)); // Convert nanoseconds to ticks
        }

        if (MaxBucketSize != -1)
        {
            builder.WithMaxBucketSize(MaxBucketSize);
        }

        builder.WithReplicas(Replicas);
        builder.WithStorage(ConvertStorage(Storage));

        return builder;
    }

    /// <summary>
    /// Loads a KV configuration from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A KVConfigJson instance.</returns>
    public static KVConfigJson FromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        return JsonSerializer.Deserialize<KVConfigJson>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize KV configuration");
    }

    /// <summary>
    /// Loads a KV configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>A KVConfigJson instance.</returns>
    public static async Task<KVConfigJson> FromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"KV configuration file not found: {filePath}", filePath);
        }

        var json = await File.ReadAllTextAsync(filePath);
        return FromJson(json);
    }

    /// <summary>
    /// Loads multiple KV configurations from a directory.
    /// </summary>
    /// <param name="directoryPath">The directory containing JSON configuration files.</param>
    /// <param name="pattern">The file pattern to match (default: *.json).</param>
    /// <returns>A list of KVConfigJson instances.</returns>
    public static async Task<List<KVConfigJson>> FromDirectoryAsync(string directoryPath, string pattern = "*.json")
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"KV configuration directory not found: {directoryPath}");
        }

        var configs = new List<KVConfigJson>();
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
                throw new InvalidOperationException($"Failed to load KV configuration from {file}: {ex.Message}", ex);
            }
        }

        return configs;
    }

    private static NatsKVStorageType ConvertStorage(KVStorageTypeJson storage) => storage switch
    {
        KVStorageTypeJson.File => NatsKVStorageType.File,
        KVStorageTypeJson.Memory => NatsKVStorageType.Memory,
        _ => NatsKVStorageType.File
    };
}

/// <summary>
/// JSON enum for KV storage type.
/// </summary>
public enum KVStorageTypeJson
{
    [JsonPropertyName("file")]
    File,

    [JsonPropertyName("memory")]
    Memory
}
