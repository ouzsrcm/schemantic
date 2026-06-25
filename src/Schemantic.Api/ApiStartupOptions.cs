using System.Text.Json;
using Schemantic.Core.Filtering;

namespace Schemantic.Api;

/// <summary>
/// Startup options for the API host (CLI arguments with configuration fallback).
/// </summary>
public sealed class ApiStartupOptions
{
    /// <summary>Provider key (e.g. <c>sqlite</c>).</summary>
    public required string Provider { get; init; }

    /// <summary>Database connection string.</summary>
    public required string ConnectionString { get; init; }

    /// <summary>Oracle schema owner; ignored by other providers.</summary>
    public string? SchemaOwner { get; init; }

    /// <summary>Optional include/exclude filter applied after introspection.</summary>
    public SchemaFilterOptions? FilterOptions { get; init; }

    /// <summary>Default page size when the client omits <c>pageSize</c>.</summary>
    public int DefaultPageSize { get; init; } = 50;

    /// <summary>Maximum allowed <c>pageSize</c> (mandatory paging cap).</summary>
    public int MaxPageSize { get; init; } = 1000;

    /// <summary>
    /// Parses options from <paramref name="args"/> and <paramref name="configuration"/>.
    /// CLI flags override configuration keys under <c>Schemantic:</c>.
    /// </summary>
    /// <param name="configuration">Application configuration (e.g. appsettings).</param>
    /// <param name="args">Command-line arguments (<c>--provider</c>, <c>--connection</c>, etc.).</param>
    /// <returns>Resolved startup options.</returns>
    public static ApiStartupOptions From(IConfiguration configuration, string[] args)
    {
        var map = ParseArgs(args);

        var provider = map.GetValueOrDefault("provider")
            ?? configuration["Schemantic:Provider"]
            ?? "sqlite";

        var connection = map.GetValueOrDefault("connection")
            ?? configuration["Schemantic:Connection"];

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "Connection string is required. Pass --connection or set Schemantic:Connection.");
        }

        var schemaOwner = map.GetValueOrDefault("schema")
            ?? configuration["Schemantic:Schema"];

        var configPath = map.GetValueOrDefault("config")
            ?? configuration["Schemantic:Config"];

        SchemaFilterOptions? filterOptions = null;
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            var json = File.ReadAllText(configPath);
            filterOptions = JsonSerializer.Deserialize<SchemaFilterOptions>(json, ConfigJsonOptions)
                ?? new SchemaFilterOptions();
        }

        return new ApiStartupOptions
        {
            Provider = provider,
            ConnectionString = connection,
            SchemaOwner = schemaOwner,
            FilterOptions = filterOptions,
        };
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = args[i][2..];
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                map[key] = args[i + 1];
                i++;
            }
            else
            {
                map[key] = "true";
            }
        }

        return map;
    }

    private static readonly JsonSerializerOptions ConfigJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}
