using System.Text;
using System.Text.RegularExpressions;
using DotGnatly.Core.Configuration;

namespace DotGnatly.Core.Parsers;

/// <summary>
/// Parser for NATS server configuration files.
/// Converts NATS config format to BrokerConfiguration instances.
/// </summary>
public class NatsConfigParser
{
    private static readonly Regex SizeUnitRegex = new(@"^(\d+(?:\.\d+)?)\s*(B|KB?|MB?|GB?|TB?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TimeUnitRegex = new(@"^(\d+(?:\.\d+)?)\s*(ns|us|ms|s|m|h)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ListenRegex = new(@"^([\w\.\-]+):(\d+)$", RegexOptions.Compiled);

    /// <summary>
    /// Parses a NATS configuration file and returns a BrokerConfiguration instance.
    /// </summary>
    /// <param name="filePath">Path to the NATS config file</param>
    /// <returns>A BrokerConfiguration instance with parsed values</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist</exception>
    public static BrokerConfiguration ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        var content = File.ReadAllText(filePath);
        return Parse(content);
    }

    /// <summary>
    /// Parses NATS configuration content and returns a BrokerConfiguration instance.
    /// </summary>
    /// <param name="content">The NATS config file content</param>
    /// <returns>A BrokerConfiguration instance with parsed values</returns>
    public static BrokerConfiguration Parse(string content)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        var config = new BrokerConfiguration();
        var lines = content.Split('\n', StringSplitOptions.None);
        var context = new ParseContext(lines);

        while (context.HasMore())
        {
            var line = context.CurrentLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                context.MoveNext();
                continue;
            }

            // Parse top-level key-value pairs
            if (TryParseKeyValue(line, out var key, out var value))
            {
                ApplyTopLevelProperty(config, key, value);
                context.MoveNext();
            }
            // Parse blocks (jetstream, accounts, leafnodes, etc.)
            else if (TryParseBlockStart(line, out var blockName))
            {
                var blockContent = ExtractBlock(context);
                ApplyBlock(config, blockName, blockContent);
            }
            else
            {
                context.MoveNext();
            }
        }

        return config;
    }

    private static void ApplyTopLevelProperty(BrokerConfiguration config, string key, string value)
    {
        switch (key.ToLowerInvariant())
        {
            case "listen":
                ParseListenAddress(value, config);
                break;
            case "server_name":
                config.ServerName = UnquoteString(value);
                break;
            case "monitor_port":
                config.HttpPort = ParseInt(value);
                break;
            case "debug":
                config.Debug = ParseBool(value);
                break;
            case "trace":
                config.Trace = ParseBool(value);
                break;
            case "log_file":
                config.LogFile = UnquoteString(value);
                break;
            case "logfile_size_limit":
                config.LogFileSize = ParseSize(value);
                break;
            case "logfile_max_num":
                config.LogFileMaxNum = ParseInt(value);
                break;
            case "max_payload":
                config.MaxPayload = ParseSize(value);
                break;
            case "write_deadline":
                config.WriteDeadline = ParseTimeSeconds(value);
                break;
            case "disable_sublist_cache":
                config.DisableSublistCache = ParseBool(value);
                break;
            case "system_account":
                config.SystemAccount = UnquoteString(value);
                break;
            case "jetstream":
                // Handle simple "jetstream: enabled" or "jetstream: true"
                if (value.Equals("enabled", StringComparison.OrdinalIgnoreCase) || ParseBool(value))
                {
                    config.Jetstream = true;
                }
                break;
        }
    }

    private static void ApplyBlock(BrokerConfiguration config, string blockName, string blockContent)
    {
        switch (blockName.ToLowerInvariant())
        {
            case "jetstream":
                ParseJetStreamBlock(blockContent, config);
                break;
            case "leafnodes":
                ParseLeafNodesBlock(blockContent, config);
                break;
            case "accounts":
                ParseAccountsBlock(blockContent, config);
                break;
        }
    }

    private static void ParseJetStreamBlock(string content, BrokerConfiguration config)
    {
        config.Jetstream = true;
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;

            if (TryParseKeyValue(trimmed, out var key, out var value))
            {
                switch (key.ToLowerInvariant())
                {
                    case "store_dir":
                        config.JetstreamStoreDir = UnquoteString(value);
                        break;
                    case "domain":
                        config.JetstreamDomain = UnquoteString(value);
                        break;
                    case "max_memory":
                    case "max_memory_store":
                        config.JetstreamMaxMemory = ParseSize(value);
                        break;
                    case "max_file":
                    case "max_file_store":
                        config.JetstreamMaxStore = ParseSize(value);
                        break;
                    case "unique_tag":
                        config.JetstreamUniqueTag = UnquoteString(value);
                        break;
                }
            }
        }
    }

    private static void ParseLeafNodesBlock(string content, BrokerConfiguration config)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var context = new ParseContext(lines);

        while (context.HasMore())
        {
            var line = context.CurrentLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                context.MoveNext();
                continue;
            }

            if (TryParseKeyValue(line, out var key, out var value))
            {
                switch (key.ToLowerInvariant())
                {
                    case "port":
                        config.LeafNode.Port = ParseInt(value);
                        break;
                    case "host":
                        config.LeafNode.Host = UnquoteString(value);
                        break;
                    case "advertise":
                        config.LeafNode.Advertise = UnquoteString(value);
                        break;
                    case "isolate_leafnode_interest":
                        config.LeafNode.IsolateLeafnodeInterest = ParseBool(value);
                        break;
                    case "reconnect_delay":
                        config.LeafNode.ReconnectDelay = value;
                        break;
                }
                context.MoveNext();
            }
            else if (TryParseBlockStart(line, out var blockName))
            {
                string blockContent;

                // Check if this is an inline block (all on one line)
                if (IsInlineBlock(line))
                {
                    blockContent = ExtractInlineBlockContent(line);
                    context.MoveNext();
                }
                else
                {
                    // Determine if we're extracting a block {...} or array [...]
                    if (line.Contains('['))
                    {
                        blockContent = ExtractArray(context);
                    }
                    else
                    {
                        blockContent = ExtractBlock(context);
                    }
                }

                switch (blockName.ToLowerInvariant())
                {
                    case "tls":
                        config.LeafNode.Tls = ParseTlsBlock(blockContent);
                        break;
                    case "authorization":
                        config.LeafNode.Authorization = ParseAuthorizationBlock(blockContent);
                        break;
                    case "remotes":
                        ParseRemotesArray(blockContent, config.LeafNode);
                        break;
                }
            }
            else
            {
                context.MoveNext();
            }
        }
    }

    private static void ParseListenAddress(string value, BrokerConfiguration config)
    {
        var match = ListenRegex.Match(value);
        if (match.Success)
        {
            config.Host = match.Groups[1].Value;
            config.Port = int.Parse(match.Groups[2].Value);
        }
    }

    private static bool TryParseKeyValue(string line, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        // Remove inline comments
        var commentIndex = line.IndexOf('#');
        if (commentIndex > 0)
        {
            line = line.Substring(0, commentIndex).Trim();
        }

        var colonIndex = line.IndexOf(':');
        var equalsIndex = line.IndexOf('=');

        int separatorIndex;
        if (colonIndex >= 0 && (equalsIndex < 0 || colonIndex < equalsIndex))
        {
            separatorIndex = colonIndex;
        }
        else if (equalsIndex >= 0)
        {
            separatorIndex = equalsIndex;
        }
        else
        {
            return false;
        }

        key = line.Substring(0, separatorIndex).Trim();
        value = line.Substring(separatorIndex + 1).Trim();

        // Reject lines that are block/array starts
        // 1. Inline blocks: "authorization {timeout: 60}" - key contains '{'
        // 2. Multi-line blocks: "SYS: {" - value starts with '{'
        // 3. Multi-line arrays: "users: [" (no closing bracket) - incomplete array
        if (key.Contains('{') || value.StartsWith("{"))
        {
            return false;
        }

        // Reject multi-line array starts (value is just "[" or "[ " without closing bracket)
        if (value.StartsWith("[") && !value.Contains("]"))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(key);
    }

    private static bool TryParseBlockStart(string line, out string blockName)
    {
        blockName = string.Empty;

        // Check if line has an opening brace or bracket
        var openBraceIndex = line.IndexOf('{');
        var openBracketIndex = line.IndexOf('[');

        int openIndex = -1;
        if (openBraceIndex >= 0 && openBracketIndex >= 0)
        {
            openIndex = Math.Min(openBraceIndex, openBracketIndex);
        }
        else if (openBraceIndex >= 0)
        {
            openIndex = openBraceIndex;
        }
        else if (openBracketIndex >= 0)
        {
            openIndex = openBracketIndex;
        }

        if (openIndex < 0)
        {
            return false;
        }

        // Extract the block name (everything before the opening brace/bracket)
        blockName = line.Substring(0, openIndex).Trim();
        // Remove trailing colon or equals if present
        if (blockName.EndsWith(":") || blockName.EndsWith("="))
        {
            blockName = blockName.Substring(0, blockName.Length - 1).Trim();
        }
        return !string.IsNullOrWhiteSpace(blockName);
    }

    private static bool IsInlineBlock(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Contains('{') && trimmed.TrimEnd().EndsWith("}");
    }

    private static string ExtractInlineBlockContent(string line)
    {
        var openBraceIndex = line.IndexOf('{');
        var closeBraceIndex = line.LastIndexOf('}');

        if (openBraceIndex >= 0 && closeBraceIndex > openBraceIndex)
        {
            return line.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1);
        }

        return string.Empty;
    }

    private static string ExtractBlock(ParseContext context)
    {
        var sb = new StringBuilder();
        var braceCount = 1; // We already encountered the opening brace
        context.MoveNext();

        while (context.HasMore() && braceCount > 0)
        {
            var line = context.CurrentLine;

            // Count braces
            foreach (var ch in line)
            {
                if (ch == '{') braceCount++;
                if (ch == '}') braceCount--;
            }

            if (braceCount > 0)
            {
                sb.AppendLine(line);
            }

            context.MoveNext();
        }

        return sb.ToString();
    }

    private static string ExtractArray(ParseContext context)
    {
        var sb = new StringBuilder();
        var bracketCount = 1; // We already encountered the opening bracket
        context.MoveNext();

        while (context.HasMore() && bracketCount > 0)
        {
            var line = context.CurrentLine;

            // Count brackets
            foreach (var ch in line)
            {
                if (ch == '[') bracketCount++;
                if (ch == ']') bracketCount--;
            }

            if (bracketCount > 0)
            {
                sb.AppendLine(line);
            }

            context.MoveNext();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses a size value with units (e.g., "8MB", "100Mb", "1GB") into bytes.
    /// </summary>
    public static long ParseSize(string value)
    {
        value = value.Trim();
        var match = SizeUnitRegex.Match(value);

        if (!match.Success)
        {
            // Try parsing as plain number
            if (long.TryParse(value, out var bytes))
                return bytes;
            return 0;
        }

        var number = double.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToUpperInvariant();

        return unit switch
        {
            "B" => (long)number,
            "K" or "KB" => (long)(number * 1024),
            "M" or "MB" => (long)(number * 1024 * 1024),
            "G" or "GB" => (long)(number * 1024 * 1024 * 1024),
            "T" or "TB" => (long)(number * 1024 * 1024 * 1024 * 1024),
            _ => 0
        };
    }

    /// <summary>
    /// Parses a time value with units (e.g., "10s", "2m", "1h") into seconds.
    /// </summary>
    public static int ParseTimeSeconds(string value)
    {
        value = value.Trim();
        var match = TimeUnitRegex.Match(value);

        if (!match.Success)
        {
            // Try parsing as plain number (assume seconds)
            if (int.TryParse(value, out var seconds))
                return seconds;
            return 0;
        }

        var number = double.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "ns" => (int)(number / 1_000_000_000),
            "us" => (int)(number / 1_000_000),
            "ms" => (int)(number / 1000),
            "s" => (int)number,
            "m" => (int)(number * 60),
            "h" => (int)(number * 3600),
            _ => 0
        };
    }

    private static int ParseInt(string value)
    {
        value = UnquoteString(value);
        if (int.TryParse(value, out var result))
            return result;
        return 0;
    }

    private static bool ParseBool(string value)
    {
        value = UnquoteString(value).ToLowerInvariant();
        return value is "true" or "enabled" or "yes" or "1";
    }

    private static string UnquoteString(string value)
    {
        value = value.Trim();
        if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
            (value.StartsWith("'") && value.EndsWith("'")))
        {
            return value.Substring(1, value.Length - 2);
        }
        return value;
    }

    private static void ParseAccountsBlock(string content, BrokerConfiguration config)
    {
        var lines = content.Split('\n', StringSplitOptions.None);
        var context = new ParseContext(lines);

        while (context.HasMore())
        {
            var line = context.CurrentLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                context.MoveNext();
                continue;
            }

            // Check if this is an account name followed by a brace
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0 && line.TrimEnd().EndsWith("{"))
            {
                var accountName = line.Substring(0, colonIndex).Trim();
                var accountContent = ExtractBlock(context);
                var account = ParseAccountBlock(accountName, accountContent);
                config.Accounts.Add(account);
            }
            else
            {
                context.MoveNext();
            }
        }
    }

    private static AccountConfiguration ParseAccountBlock(string name, string content)
    {
        var account = new AccountConfiguration { Name = name };
        var lines = content.Split('\n', StringSplitOptions.None);
        var context = new ParseContext(lines);

        while (context.HasMore())
        {
            var line = context.CurrentLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                context.MoveNext();
                continue;
            }

            if (TryParseKeyValue(line, out var key, out var value))
            {
                switch (key.ToLowerInvariant())
                {
                    case "jetstream":
                        account.Jetstream = value.Equals("enabled", StringComparison.OrdinalIgnoreCase) ||
                                          ParseBool(value);
                        break;
                    case "users":
                        // Handle single-line arrays: users: [ {...} ]
                        account.Users = ParseUsersArray(value);
                        break;
                    case "imports":
                        // Handle single-line arrays: imports: [ {...} ]
                        account.Imports = ParseImportsExportsArray(value);
                        break;
                    case "exports":
                        // Handle single-line arrays: exports: [ {...} ]
                        account.Exports = ParseImportsExportsArray(value);
                        break;
                }
                context.MoveNext();
            }
            else if (TryParseBlockStart(line, out var blockName))
            {
                // Determine if we're extracting a block {...} or array [...]
                var blockContent = line.Contains('[') ? ExtractArray(context) : ExtractBlock(context);
                switch (blockName.ToLowerInvariant())
                {
                    case "users":
                        account.Users = ParseUsersArray(blockContent);
                        break;
                    case "imports":
                        account.Imports = ParseImportsExportsArray(blockContent);
                        break;
                    case "exports":
                        account.Exports = ParseImportsExportsArray(blockContent);
                        break;
                    case "mappings":
                        account.Mappings = ParseMappingsBlock(blockContent);
                        break;
                }
            }
            else
            {
                context.MoveNext();
            }
        }

        return account;
    }

    private static List<UserConfiguration> ParseUsersArray(string content)
    {
        var users = new List<UserConfiguration>();

        // Remove array brackets and split by objects
        content = content.Trim();
        if (content.StartsWith("["))
            content = content.Substring(1);
        if (content.EndsWith("]"))
            content = content.Substring(0, content.Length - 1);

        // Simple parsing of user objects
        var userMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"\{([^}]+)\}",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        foreach (System.Text.RegularExpressions.Match match in userMatches)
        {
            var userContent = match.Groups[1].Value;
            var user = ParseUserObject(userContent);
            users.Add(user);
        }

        return users;
    }

    private static UserConfiguration ParseUserObject(string content)
    {
        var user = new UserConfiguration();

        // Parse key-value pairs within the user object
        var pairs = content.Split(',');
        foreach (var pair in pairs)
        {
            if (TryParseKeyValue(pair, out var key, out var value))
            {
                switch (key.ToLowerInvariant())
                {
                    case "user":
                        user.User = UnquoteString(value);
                        break;
                    case "password":
                        user.Password = UnquoteString(value);
                        break;
                    case "account":
                        user.Account = UnquoteString(value);
                        break;
                }
            }
        }

        return user;
    }

    private static List<ImportExportConfiguration> ParseImportsExportsArray(string content)
    {
        var items = new List<ImportExportConfiguration>();

        // Remove array brackets
        content = content.Trim();
        if (content.StartsWith("["))
            content = content.Substring(1);
        if (content.EndsWith("]"))
            content = content.Substring(0, content.Length - 1);

        // Parse each import/export object
        var objectMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"\{([^}]+)\}",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        foreach (System.Text.RegularExpressions.Match match in objectMatches)
        {
            var objectContent = match.Groups[1].Value;
            var item = ParseImportExportObject(objectContent);
            items.Add(item);
        }

        return items;
    }

    private static ImportExportConfiguration ParseImportExportObject(string content)
    {
        var item = new ImportExportConfiguration();

        // Determine if this is a stream or service
        if (content.Contains("stream:") || content.Contains("stream "))
            item.Type = "stream";
        else if (content.Contains("service:") || content.Contains("service "))
            item.Type = "service";

        // Parse the content
        var pairs = content.Split(',');
        foreach (var pair in pairs)
        {
            if (TryParseKeyValue(pair, out var key, out var value))
            {
                switch (key.ToLowerInvariant())
                {
                    case "stream":
                        item.Type = "stream";
                        item.Subject = UnquoteString(value);
                        break;
                    case "service":
                        item.Type = "service";
                        item.Subject = UnquoteString(value);
                        break;
                    case "subject":
                        item.Subject = UnquoteString(value);
                        break;
                    case "account":
                        item.Account = UnquoteString(value);
                        break;
                    case "to":
                        item.To = UnquoteString(value);
                        break;
                    case "response_type":
                        item.ResponseType = UnquoteString(value);
                        break;
                    case "response_threshold":
                        item.ResponseThreshold = UnquoteString(value);
                        break;
                }
            }
            else if (pair.Contains(":"))
            {
                // Handle nested objects like {account: SYS, subject: "..."}
                var nestedMatch = System.Text.RegularExpressions.Regex.Match(
                    pair,
                    @"(stream|service)\s*:\s*\{([^}]+)\}"
                );
                if (nestedMatch.Success)
                {
                    item.Type = nestedMatch.Groups[1].Value;
                    var nestedContent = nestedMatch.Groups[2].Value;
                    var nestedPairs = nestedContent.Split(',');
                    foreach (var nestedPair in nestedPairs)
                    {
                        if (TryParseKeyValue(nestedPair, out var nk, out var nv))
                        {
                            switch (nk.ToLowerInvariant())
                            {
                                case "subject":
                                    item.Subject = UnquoteString(nv);
                                    break;
                                case "account":
                                    item.Account = UnquoteString(nv);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        return item;
    }

    private static Dictionary<string, string> ParseMappingsBlock(string content)
    {
        var mappings = new Dictionary<string, string>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                continue;

            if (TryParseKeyValue(trimmed, out var key, out var value))
            {
                mappings[UnquoteString(key)] = UnquoteString(value);
            }
        }

        return mappings;
    }

    private static TlsConfiguration ParseTlsBlock(string content)
    {
        var tls = new TlsConfiguration();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var context = new ParseContext(lines);

        while (context.HasMore())
        {
            var line = context.CurrentLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                context.MoveNext();
                continue;
            }

            if (TryParseKeyValue(line, out var key, out var value))
            {
                switch (key.ToLowerInvariant())
                {
                    case "cert_file":
                        tls.CertFile = UnquoteString(value);
                        break;
                    case "key_file":
                        tls.KeyFile = UnquoteString(value);
                        break;
                    case "ca_cert_file":
                    case "ca_file":
                        tls.CaCertFile = UnquoteString(value);
                        break;
                    case "verify_client_certs":
                    case "verify":
                        tls.VerifyClientCerts = ParseBool(value);
                        break;
                    case "timeout":
                        tls.Timeout = ParseInt(value);
                        break;
                    case "handshake_first":
                        tls.HandshakeFirst = ParseBool(value);
                        break;
                    case "insecure":
                        tls.Insecure = ParseBool(value);
                        break;
                    case "cert_store":
                        tls.CertStore = UnquoteString(value);
                        break;
                    case "cert_match_by":
                        tls.CertMatchBy = UnquoteString(value);
                        break;
                    case "cert_match":
                        tls.CertMatch = UnquoteString(value);
                        break;
                    case "pinned_certs":
                        // Handle single-line arrays: pinned_certs: ["cert1", "cert2"]
                        tls.PinnedCerts = ParseStringArray(value);
                        break;
                }
                context.MoveNext();
            }
            else if (TryParseBlockStart(line, out var blockName) && blockName.ToLowerInvariant() == "pinned_certs")
            {
                // Handle multi-line arrays: pinned_certs: [ ... ]
                var blockContent = line.Contains('[') ? ExtractArray(context) : ExtractBlock(context);
                tls.PinnedCerts = ParseStringArray(blockContent);
            }
            else
            {
                context.MoveNext();
            }
        }

        return tls;
    }

    private static AuthorizationConfiguration ParseAuthorizationBlock(string content)
    {
        var auth = new AuthorizationConfiguration();

        // Check if content is inline (comma-separated) or multi-line
        var isInline = content.Contains(',') && !content.Contains('\n');

        if (isInline)
        {
            // Parse comma-separated inline content
            var pairs = content.Split(',');
            foreach (var pair in pairs)
            {
                if (TryParseKeyValue(pair, out var key, out var value))
                {
                    switch (key.ToLowerInvariant())
                    {
                        case "timeout":
                            auth.Timeout = ParseInt(value);
                            break;
                        case "user":
                            auth.User = UnquoteString(value);
                            break;
                        case "password":
                            auth.Password = UnquoteString(value);
                            break;
                        case "account":
                            auth.Account = UnquoteString(value);
                            break;
                        case "token":
                            auth.Token = UnquoteString(value);
                            break;
                    }
                }
            }
        }
        else
        {
            // Parse multi-line content
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var context = new ParseContext(lines);

            while (context.HasMore())
            {
                var line = context.CurrentLine.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    context.MoveNext();
                    continue;
                }

                // Check for blocks BEFORE key-value pairs
                if (TryParseBlockStart(line, out var blockName) && blockName.ToLowerInvariant() == "users")
                {
                    // Determine if we're extracting a block {...} or array [...]
                    var blockContent = line.Contains('[') ? ExtractArray(context) : ExtractBlock(context);
                    auth.Users = ParseUsersArray(blockContent);
                }
                else if (TryParseKeyValue(line, out var key, out var value))
                {
                    switch (key.ToLowerInvariant())
                    {
                        case "timeout":
                            auth.Timeout = ParseInt(value);
                            break;
                        case "user":
                            auth.User = UnquoteString(value);
                            break;
                        case "password":
                            auth.Password = UnquoteString(value);
                            break;
                        case "account":
                            auth.Account = UnquoteString(value);
                            break;
                        case "token":
                            auth.Token = UnquoteString(value);
                            break;
                        case "users":
                            // Handle single-line arrays: users: [ {...} ]
                            auth.Users = ParseUsersArray(value);
                            break;
                    }
                    context.MoveNext();
                }
                else
                {
                    context.MoveNext();
                }
            }
        }

        return auth;
    }

    private static void ParseRemotesArray(string content, LeafNodeConfiguration leafNode)
    {
        // Remove array brackets
        content = content.Trim();
        if (content.StartsWith("["))
            content = content.Substring(1);
        if (content.EndsWith("]"))
            content = content.Substring(0, content.Length - 1);

        // Parse each remote object
        var objectMatches = System.Text.RegularExpressions.Regex.Matches(
            content,
            @"\{([^}]*(?:\{[^}]*\}[^}]*)*)\}",
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        foreach (System.Text.RegularExpressions.Match match in objectMatches)
        {
            var objectContent = match.Groups[1].Value;
            var remote = ParseRemoteObject(objectContent);
            leafNode.Remotes.Add(remote);
        }
    }

    private static LeafNodeRemoteConfiguration ParseRemoteObject(string content)
    {
        var remote = new LeafNodeRemoteConfiguration();
        var lines = content.Split('\n', StringSplitOptions.None);
        var context = new ParseContext(lines);

        while (context.HasMore())
        {
            var line = context.CurrentLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                context.MoveNext();
                continue;
            }

            if (TryParseKeyValue(line, out var key, out var value))
            {
                switch (key.ToLowerInvariant())
                {
                    case "urls":
                        remote.Urls = ParseStringArray(value);
                        break;
                    case "account":
                        remote.Account = UnquoteString(value);
                        break;
                    case "credentials":
                        remote.Credentials = UnquoteString(value);
                        break;
                    case "first_info_timeout":
                        remote.FirstInfoTimeout = value;
                        break;
                }
                context.MoveNext();
            }
            else if (TryParseBlockStart(line, out var blockName))
            {
                switch (blockName.ToLowerInvariant())
                {
                    case "tls":
                        var tlsContent = ExtractBlock(context);
                        remote.Tls = ParseTlsBlock(tlsContent);
                        break;
                    case "urls":
                        // Handle multi-line urls array
                        var urlsContent = line.Contains('[') ? ExtractArray(context) : ExtractBlock(context);
                        remote.Urls = ParseStringArray(urlsContent);
                        break;
                }
            }
            else
            {
                context.MoveNext();
            }
        }

        return remote;
    }

    private static List<string> ParseStringArray(string value)
    {
        var result = new List<string>();

        // Remove brackets
        value = value.Trim();
        if (value.StartsWith("["))
            value = value.Substring(1);
        if (value.EndsWith("]"))
            value = value.Substring(0, value.Length - 1);

        // Check if this is a comma-separated or newline-separated array
        char separator = value.Contains(',') ? ',' : '\n';

        // Split and clean up
        var items = value.Split(separator);
        foreach (var item in items)
        {
            var cleaned = UnquoteString(item.Trim());
            if (!string.IsNullOrWhiteSpace(cleaned))
            {
                result.Add(cleaned);
            }
        }

        return result;
    }

    private class ParseContext
    {
        private readonly string[] _lines;
        private int _index;

        public ParseContext(string[] lines)
        {
            _lines = lines;
            _index = 0;
        }

        public string CurrentLine => _index < _lines.Length ? _lines[_index] : string.Empty;
        public bool HasMore() => _index < _lines.Length;
        public void MoveNext() => _index++;
    }
}
