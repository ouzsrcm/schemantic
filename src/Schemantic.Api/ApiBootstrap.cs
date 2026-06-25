using Schemantic.Core.Abstractions;
using Schemantic.Core.Filtering;
using Schemantic.Core.Model;
using Schemantic.Providers.Oracle;
using Schemantic.Providers.Sqlite;
using Schemantic.Providers.SqlServer;

namespace Schemantic.Api;

/// <summary>
/// Composition root: resolves provider + dialect pairs and loads the filtered schema at startup.
/// </summary>
public static class ApiBootstrap
{
    private static readonly Dictionary<string, Func<string?, (IDatabaseProvider Provider, ISqlDialect Dialect)>>
        Registrations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["sqlserver"] = _ => (new SqlServerProvider(), new UnsupportedSqlDialect("SQL Server")),
            ["oracle"] = schema => (new OracleProvider(schema), new UnsupportedSqlDialect("Oracle")),
            ["sqlite"] = _ => (new SqliteProvider(), new SqliteSqlDialect()),
        };

    /// <summary>Loaded runtime context shared by endpoints.</summary>
    public sealed class RuntimeContext
    {
        /// <summary>Database provider used for introspection and connections.</summary>
        public required IDatabaseProvider Provider { get; init; }

        /// <summary>SQL dialect for parameterized data queries.</summary>
        public required ISqlDialect Dialect { get; init; }

        /// <summary>Filtered schema loaded at startup.</summary>
        public required DatabaseSchema Schema { get; init; }

        /// <summary>Connection string for data queries.</summary>
        public required string ConnectionString { get; init; }

        /// <summary>Resolved startup options.</summary>
        public required ApiStartupOptions Options { get; init; }
    }

    /// <summary>
    /// Resolves the provider, introspects the database, applies optional filtering, and returns runtime context.
    /// </summary>
    /// <param name="options">Startup options (provider, connection, optional filter config).</param>
    /// <param name="cancellationToken">Token used to cancel introspection.</param>
    /// <returns>Runtime context shared by API endpoints.</returns>
    public static async Task<RuntimeContext> LoadAsync(
        ApiStartupOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!Registrations.TryGetValue(options.Provider, out var create))
        {
            var available = string.Join(", ", Registrations.Keys);
            throw new InvalidOperationException(
                $"Unknown provider '{options.Provider}'. Available providers: {available}.");
        }

        var (provider, dialect) = create(options.SchemaOwner);
        var schema = await provider.ReadSchemaAsync(options.ConnectionString, cancellationToken)
            .ConfigureAwait(false);

        if (options.FilterOptions is { } filter)
        {
            SchemaFilter.Apply(schema, filter);
        }

        return new RuntimeContext
        {
            Provider = provider,
            Dialect = dialect,
            Schema = schema,
            ConnectionString = options.ConnectionString,
            Options = options,
        };
    }
}
