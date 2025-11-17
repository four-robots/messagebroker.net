using System;
using System.IO;
using System.Text.Json;
using DotGnatly.Core.Configuration;
using DotGnatly.Core.Parsers;

namespace DotGnatly.Examples.ConfigParser;

/// <summary>
/// Example demonstrating parsing NATS configuration files into BrokerConfiguration.
/// </summary>
public static class ConfigParserExample
{
    public static void Run()
    {
        Console.WriteLine("=== NATS Config Parser Example ===\n");

        var configDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "test-configs"
        );

        // Normalize the path
        configDir = Path.GetFullPath(configDir);

        if (!Directory.Exists(configDir))
        {
            Console.WriteLine($"Config directory not found: {configDir}");
            Console.WriteLine("Please ensure test-configs directory exists with config files.");
            return;
        }

        var configFiles = new[]
        {
            "basic.conf",
            "leaf.conf",
            "hub.conf"
        };

        foreach (var configFile in configFiles)
        {
            var filePath = Path.Combine(configDir, configFile);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"⚠ Config file not found: {configFile}\n");
                continue;
            }

            try
            {
                Console.WriteLine($"📄 Parsing: {configFile}");
                Console.WriteLine(new string('-', 60));

                var config = NatsConfigParser.ParseFile(filePath);

                // Display parsed configuration
                DisplayConfiguration(config);

                // Optionally, export as JSON
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Save to a JSON file
                var jsonOutputPath = Path.Combine(configDir, $"{Path.GetFileNameWithoutExtension(configFile)}.json");
                File.WriteAllText(jsonOutputPath, json);
                Console.WriteLine($"✓ Exported JSON to: {Path.GetFileName(jsonOutputPath)}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error parsing {configFile}: {ex.Message}");
                Console.WriteLine();
            }
        }

        Console.WriteLine("=== Parsing Complete ===");
    }

    private static void DisplayConfiguration(BrokerConfiguration config)
    {
        Console.WriteLine($"  Server Name:         {config.ServerName ?? "(not set)"}");
        Console.WriteLine($"  Listen Address:      {config.Host}:{config.Port}");
        Console.WriteLine($"  Monitor Port:        {config.HttpPort}");
        Console.WriteLine($"  Debug:               {config.Debug}");
        Console.WriteLine($"  Trace:               {config.Trace}");
        Console.WriteLine($"  Log File:            {config.LogFile ?? "(not set)"}");
        Console.WriteLine($"  Log File Size:       {FormatSize(config.LogFileSize)}");
        Console.WriteLine($"  Log File Max Num:    {config.LogFileMaxNum}");
        Console.WriteLine($"  Max Payload:         {FormatSize(config.MaxPayload)}");
        Console.WriteLine($"  Write Deadline:      {config.WriteDeadline}s");
        Console.WriteLine($"  Disable Sublist:     {config.DisableSublistCache}");
        Console.WriteLine($"  System Account:      {config.SystemAccount ?? "(not set)"}");
        Console.WriteLine();
        Console.WriteLine("  JetStream:");
        Console.WriteLine($"    Enabled:           {config.Jetstream}");
        Console.WriteLine($"    Store Dir:         {config.JetstreamStoreDir}");
        Console.WriteLine($"    Domain:            {config.JetstreamDomain ?? "(not set)"}");
        Console.WriteLine($"    Max Memory:        {(config.JetstreamMaxMemory == -1 ? "unlimited" : FormatSize(config.JetstreamMaxMemory))}");
        Console.WriteLine($"    Max Store:         {(config.JetstreamMaxStore == -1 ? "unlimited" : FormatSize(config.JetstreamMaxStore))}");
        Console.WriteLine();
        Console.WriteLine("  Leaf Node:");
        Console.WriteLine($"    Port:              {config.LeafNode.Port}");
        Console.WriteLine($"    Host:              {config.LeafNode.Host}");
        Console.WriteLine();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes == 0) return "0 B";
        if (bytes < 0) return "unlimited";

        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}
