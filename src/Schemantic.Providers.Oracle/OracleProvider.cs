using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Providers.Oracle;

/// <summary>
/// Reads Oracle data dictionary metadata and maps it to the shared <see cref="DatabaseSchema"/> model.
/// </summary>
public sealed class OracleProvider : IDatabaseProvider
{
    /// <summary>
    /// Creates an Oracle provider that reads metadata for the given schema owner.
    /// When <paramref name="owner"/> is null, the connected user's schema is used.
    /// </summary>
    public OracleProvider(string? owner = null)
    {
        Owner = owner;
    }

    /// <summary>
    /// Oracle schema owner to read metadata for. When null, resolved at connect time via <c>SELECT USER FROM DUAL</c>.
    /// </summary>
    public string? Owner { get; set; }

    /// <inheritdoc />
    public string Name => "Oracle";

    /// <inheritdoc />
    public DbConnection CreateConnection(string connectionString) =>
        throw new NotImplementedException("CreateConnection for Oracle will be added in a future release.");

    /// <inheritdoc />
    public async Task<DatabaseSchema> ReadSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        try
        {
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var owner = await ResolveOwnerAsync(connection, cancellationToken).ConfigureAwait(false);

            var schema = new DatabaseSchema
            {
                DatabaseName = await ReadDatabaseNameAsync(connection, cancellationToken)
                    .ConfigureAwait(false),
            };

            var tables = await ReadTablesAsync(connection, owner, cancellationToken)
                .ConfigureAwait(false);
            await PopulateColumnsAsync(connection, owner, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulatePrimaryKeysAsync(connection, owner, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulateForeignKeysAsync(connection, owner, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulateIndexesAsync(connection, owner, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulateTableDescriptionsAsync(connection, owner, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulateColumnDescriptionsAsync(connection, owner, tables, cancellationToken)
                .ConfigureAwait(false);

            schema.Tables = tables.Values
                .OrderBy(t => t.Schema, StringComparer.Ordinal)
                .ThenBy(t => t.Name, StringComparer.Ordinal)
                .ToList();

            var views = await ReadViewsAsync(connection, owner, cancellationToken)
                .ConfigureAwait(false);
            await PopulateViewColumnsAsync(connection, owner, views, cancellationToken)
                .ConfigureAwait(false);
            await PopulateViewDescriptionsAsync(connection, owner, views, cancellationToken)
                .ConfigureAwait(false);
            await PopulateViewColumnDescriptionsAsync(connection, owner, views, cancellationToken)
                .ConfigureAwait(false);

            schema.Views = views.Values
                .OrderBy(v => v.Schema, StringComparer.Ordinal)
                .ThenBy(v => v.Name, StringComparer.Ordinal)
                .ToList();

            return schema;
        }
        catch (OracleException ex)
        {
            throw new InvalidOperationException(
                $"Failed to read schema from Oracle: {ex.Message}",
                ex);
        }
    }

    private async Task<string> ResolveOwnerAsync(
        OracleConnection connection,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(Owner))
        {
            return Owner;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = OracleSchemaQueries.CurrentUser;

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        var user = result?.ToString();

        if (string.IsNullOrWhiteSpace(user))
        {
            throw new InvalidOperationException("Failed to resolve the connected Oracle user schema.");
        }

        return user;
    }

    private static async Task<string> ReadDatabaseNameAsync(
        OracleConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = OracleSchemaQueries.DatabaseName;

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result?.ToString() ?? string.Empty;
    }

    private static async Task<Dictionary<TableKey, TableInfo>> ReadTablesAsync(
        OracleConnection connection,
        string owner,
        CancellationToken cancellationToken)
    {
        var tables = new Dictionary<TableKey, TableInfo>();

        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.Tables);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var table = new TableInfo
            {
                Schema = owner,
                Name = reader.GetString(0),
            };

            tables[new TableKey(table.Schema, table.Name)] = table;
        }

        return tables;
    }

    private static async Task PopulateColumnsAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.Columns);
        command.InitialLONGFetchSize = -1;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(owner, reader.GetString(0));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            var dataType = reader.GetString(2);
            var dataLength = reader.IsDBNull(3) ? (int?)null : Convert.ToInt32(reader.GetValue(3));
            var precision = reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4));
            var scale = reader.IsDBNull(5) ? (int?)null : Convert.ToInt32(reader.GetValue(5));

            table.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(1),
                DataType = FormatDataType(dataType, precision, scale),
                IsNullable = string.Equals(reader.GetString(6), "Y", StringComparison.Ordinal),
                MaxLength = NormalizeMaxLength(dataType, dataLength),
                DefaultValue = reader.IsDBNull(7) ? null : reader.GetString(7).Trim(),
            });
        }
    }

    private static async Task PopulatePrimaryKeysAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.PrimaryKeys);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(owner, reader.GetString(0));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            var columnName = reader.GetString(1);
            var column = table.Columns.FirstOrDefault(c => c.Name == columnName);

            if (column is not null)
            {
                column.IsPrimaryKey = true;
            }
        }
    }

    private static async Task PopulateForeignKeysAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.ForeignKeys);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(owner, reader.GetString(0));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            table.ForeignKeys.Add(new ForeignKeyInfo
            {
                Name = reader.GetString(1),
                Column = reader.GetString(2),
                ReferencedSchema = reader.GetString(3),
                ReferencedTable = reader.GetString(4),
                ReferencedColumn = reader.GetString(5),
            });
        }
    }

    private static async Task PopulateIndexesAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        var indexesByTable = new Dictionary<TableKey, Dictionary<string, IndexInfo>>();

        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.Indexes);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var tableKey = new TableKey(owner, reader.GetString(0));
            if (!tables.ContainsKey(tableKey))
            {
                continue;
            }

            var indexName = reader.GetString(1);
            if (!indexesByTable.TryGetValue(tableKey, out var tableIndexes))
            {
                tableIndexes = new Dictionary<string, IndexInfo>(StringComparer.Ordinal);
                indexesByTable[tableKey] = tableIndexes;
            }

            if (!tableIndexes.TryGetValue(indexName, out var index))
            {
                index = new IndexInfo
                {
                    Name = indexName,
                    IsUnique = string.Equals(reader.GetString(2), "UNIQUE", StringComparison.Ordinal),
                };
                tableIndexes[indexName] = index;
            }

            index.Columns.Add(reader.GetString(3));
        }

        foreach (var (tableKey, tableIndexes) in indexesByTable)
        {
            if (!tables.TryGetValue(tableKey, out var table))
            {
                continue;
            }

            foreach (var index in tableIndexes.Values.OrderBy(i => i.Name, StringComparer.Ordinal))
            {
                table.Indexes.Add(index);
            }
        }
    }

    private static async Task PopulateTableDescriptionsAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.TableComments);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(owner, reader.GetString(0));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            table.Description = reader.IsDBNull(1) ? null : reader.GetString(1);
        }
    }

    private static async Task PopulateColumnDescriptionsAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.ColumnComments);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(owner, reader.GetString(0));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            var columnName = reader.GetString(1);
            var column = table.Columns.FirstOrDefault(c => c.Name == columnName);

            if (column is not null)
            {
                column.Description = reader.IsDBNull(2) ? null : reader.GetString(2);
            }
        }
    }

    private static async Task<Dictionary<TableKey, ViewInfo>> ReadViewsAsync(
        OracleConnection connection,
        string owner,
        CancellationToken cancellationToken)
    {
        var views = new Dictionary<TableKey, ViewInfo>();

        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.Views);
        command.InitialLONGFetchSize = -1;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var view = new ViewInfo
            {
                Schema = owner,
                Name = reader.GetString(0),
                Definition = reader.IsDBNull(1) ? null : reader.GetString(1),
            };

            views[new TableKey(view.Schema, view.Name)] = view;
        }

        return views;
    }

    private static async Task PopulateViewColumnsAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, ViewInfo> views,
        CancellationToken cancellationToken)
    {
        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.ViewColumns);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(owner, reader.GetString(0));
            if (!views.TryGetValue(key, out var view))
            {
                continue;
            }

            var dataType = reader.GetString(2);
            var dataLength = reader.IsDBNull(3) ? (int?)null : Convert.ToInt32(reader.GetValue(3));
            var precision = reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4));
            var scale = reader.IsDBNull(5) ? (int?)null : Convert.ToInt32(reader.GetValue(5));

            view.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(1),
                DataType = FormatDataType(dataType, precision, scale),
                IsNullable = string.Equals(reader.GetString(6), "Y", StringComparison.Ordinal),
                IsPrimaryKey = false,
                MaxLength = NormalizeMaxLength(dataType, dataLength),
            });
        }
    }

    private static async Task PopulateViewDescriptionsAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, ViewInfo> views,
        CancellationToken cancellationToken)
    {
        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.ViewComments);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(owner, reader.GetString(0));
            if (!views.TryGetValue(key, out var view))
            {
                continue;
            }

            view.Description = reader.IsDBNull(1) ? null : reader.GetString(1);
        }
    }

    private static async Task PopulateViewColumnDescriptionsAsync(
        OracleConnection connection,
        string owner,
        Dictionary<TableKey, ViewInfo> views,
        CancellationToken cancellationToken)
    {
        await using var command = CreateOwnerCommand(connection, owner, OracleSchemaQueries.ColumnComments);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(owner, reader.GetString(0));
            if (!views.TryGetValue(key, out var view))
            {
                continue;
            }

            var columnName = reader.GetString(1);
            var column = view.Columns.FirstOrDefault(c => c.Name == columnName);

            if (column is not null)
            {
                column.Description = reader.IsDBNull(2) ? null : reader.GetString(2);
            }
        }
    }

    private static OracleCommand CreateOwnerCommand(
        OracleConnection connection,
        string owner,
        string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.Parameters.Add(new OracleParameter("owner", owner));
        return command;
    }

    private static string FormatDataType(string dataType, int? precision, int? scale)
    {
        if (dataType == "NUMBER" && precision.HasValue)
        {
            return scale.HasValue
                ? $"NUMBER({precision},{scale})"
                : $"NUMBER({precision})";
        }

        if (dataType == "FLOAT" && precision.HasValue)
        {
            return $"FLOAT({precision})";
        }

        return dataType;
    }

    private static int? NormalizeMaxLength(string dataType, int? dataLength) =>
        dataType is "VARCHAR2" or "CHAR" or "NVARCHAR2" or "NCHAR" or "RAW"
            ? dataLength
            : null;

    private readonly record struct TableKey(string Schema, string Name);
}
