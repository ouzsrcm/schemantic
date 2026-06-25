using System.Data.Common;
using Microsoft.Data.Sqlite;
using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Providers.Sqlite;

/// <summary>
/// Reads SQLite catalog metadata via PRAGMA and <c>sqlite_master</c>, mapping to <see cref="DatabaseSchema"/>.
/// </summary>
public sealed class SqliteProvider : IDatabaseProvider
{
    private const string MainSchema = "main";

    /// <inheritdoc />
    public string Name => "Sqlite";

    /// <inheritdoc />
    public DbConnection CreateConnection(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return new SqliteConnection(connectionString);
    }

    /// <inheritdoc />
    public async Task<DatabaseSchema> ReadSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var schema = new DatabaseSchema
            {
                DatabaseName = connection.DataSource,
            };

            var tables = await ReadTablesAsync(connection, cancellationToken)
                .ConfigureAwait(false);

            foreach (var table in tables.Values.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
            {
                await PopulateColumnsAsync(connection, table, cancellationToken)
                    .ConfigureAwait(false);
                await PopulateForeignKeysAsync(connection, table, cancellationToken)
                    .ConfigureAwait(false);
                await PopulateIndexesAsync(connection, table, cancellationToken)
                    .ConfigureAwait(false);
            }

            schema.Tables = tables.Values
                .OrderBy(t => t.Schema, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var views = await ReadViewsAsync(connection, cancellationToken)
                .ConfigureAwait(false);

            foreach (var view in views.Values.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
            {
                await PopulateViewColumnsAsync(connection, view, cancellationToken)
                    .ConfigureAwait(false);
            }

            schema.Views = views.Values
                .OrderBy(v => v.Schema, StringComparer.OrdinalIgnoreCase)
                .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return schema;
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(
                $"Failed to read schema from SQLite: {ex.Message}",
                ex);
        }
    }

    private static async Task<Dictionary<string, TableInfo>> ReadTablesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql =
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";

        var tables = new Dictionary<string, TableInfo>(StringComparer.OrdinalIgnoreCase);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var name = reader.GetString(0);
            tables[name] = new TableInfo
            {
                Schema = MainSchema,
                Name = name,
            };
        }

        return tables;
    }

    private static async Task<Dictionary<string, ViewInfo>> ReadViewsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql =
            "SELECT name, sql FROM sqlite_master WHERE type='view' AND name NOT LIKE 'sqlite_%' ORDER BY name";

        var views = new Dictionary<string, ViewInfo>(StringComparer.OrdinalIgnoreCase);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var name = reader.GetString(0);
            views[name] = new ViewInfo
            {
                Schema = MainSchema,
                Name = name,
                Definition = reader.IsDBNull(1) ? null : reader.GetString(1),
            };
        }

        return views;
    }

    private static async Task PopulateViewColumnsAsync(
        SqliteConnection connection,
        ViewInfo view,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(view.Name)})";

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var type = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var notNull = reader.GetInt32(3);

            view.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(1),
                DataType = type,
                IsNullable = notNull == 0,
                IsPrimaryKey = false,
            });
        }
    }

    private static async Task PopulateColumnsAsync(
        SqliteConnection connection,
        TableInfo table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({QuoteIdentifier(table.Name)})";

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var type = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            var notNull = reader.GetInt32(3);
            var pk = reader.GetInt32(5);

            table.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(1),
                DataType = type,
                IsNullable = notNull == 0,
                IsPrimaryKey = pk > 0,
                DefaultValue = reader.IsDBNull(4) ? null : reader.GetValue(4)?.ToString(),
            });
        }
    }

    private static async Task PopulateForeignKeysAsync(
        SqliteConnection connection,
        TableInfo table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list({QuoteIdentifier(table.Name)})";

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var rows = new List<ForeignKeyRow>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(new ForeignKeyRow(
                Id: reader.GetInt32(0),
                Seq: reader.GetInt32(1),
                ReferencedTable: reader.GetString(2),
                Column: reader.GetString(3),
                ReferencedColumn: reader.GetString(4)));
        }

        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in rows.GroupBy(r => r.Id).OrderBy(g => g.Key))
        {
            var referencedTable = group.First().ReferencedTable;
            var fkName = GenerateForeignKeyName(table.Name, referencedTable, group.Key, usedNames);

            foreach (var row in group.OrderBy(r => r.Seq))
            {
                table.ForeignKeys.Add(new ForeignKeyInfo
                {
                    Name = fkName,
                    Column = row.Column,
                    ReferencedSchema = MainSchema,
                    ReferencedTable = row.ReferencedTable,
                    ReferencedColumn = row.ReferencedColumn,
                });
            }
        }
    }

    private static async Task PopulateIndexesAsync(
        SqliteConnection connection,
        TableInfo table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA index_list({QuoteIdentifier(table.Name)})";

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var indexEntries = new List<IndexListRow>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var indexName = reader.GetString(1);
            var origin = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);

            if (string.Equals(origin, "pk", StringComparison.OrdinalIgnoreCase)
                || indexName.StartsWith("sqlite_autoindex", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            indexEntries.Add(new IndexListRow(
                Name: indexName,
                IsUnique: reader.GetInt32(2) == 1));
        }

        foreach (var entry in indexEntries.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            var columns = await ReadIndexColumnsAsync(connection, entry.Name, cancellationToken)
                .ConfigureAwait(false);

            table.Indexes.Add(new IndexInfo
            {
                Name = entry.Name,
                IsUnique = entry.IsUnique,
                Columns = columns,
            });
        }
    }

    private static async Task<IList<string>> ReadIndexColumnsAsync(
        SqliteConnection connection,
        string indexName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA index_info({QuoteIdentifier(indexName)})";

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var columns = new List<(int SeqNo, string Name)>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var seqNo = reader.GetInt32(0);
            var name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            columns.Add((seqNo, name));
        }

        return columns
            .OrderBy(c => c.SeqNo)
            .Select(c => c.Name)
            .ToList();
    }

    private static string GenerateForeignKeyName(
        string tableName,
        string referencedTable,
        int id,
        HashSet<string> usedNames)
    {
        var baseName = $"FK_{tableName}_{referencedTable}";
        if (usedNames.Add(baseName))
        {
            return baseName;
        }

        var withId = $"{baseName}_{id}";
        usedNames.Add(withId);
        return withId;
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private readonly record struct ForeignKeyRow(
        int Id,
        int Seq,
        string ReferencedTable,
        string Column,
        string ReferencedColumn);

    private readonly record struct IndexListRow(string Name, bool IsUnique);
}
