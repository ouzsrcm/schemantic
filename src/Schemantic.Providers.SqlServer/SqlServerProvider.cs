using Microsoft.Data.SqlClient;
using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Providers.SqlServer;

/// <summary>
/// Reads SQL Server catalog metadata and maps it to the shared <see cref="DatabaseSchema"/> model.
/// </summary>
public sealed class SqlServerProvider : IDatabaseProvider
{
    private const string MsDescriptionProperty = "MS_Description";

    /// <inheritdoc />
    public string Name => "SqlServer";

    /// <inheritdoc />
    public async Task<DatabaseSchema> ReadSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var schema = new DatabaseSchema
            {
                DatabaseName = await ReadDatabaseNameAsync(connection, cancellationToken)
                    .ConfigureAwait(false),
            };

            var tables = await ReadTablesAsync(connection, cancellationToken)
                .ConfigureAwait(false);
            await PopulateColumnsAsync(connection, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulatePrimaryKeysAsync(connection, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulateForeignKeysAsync(connection, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulateIndexesAsync(connection, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulateTableDescriptionsAsync(connection, tables, cancellationToken)
                .ConfigureAwait(false);
            await PopulateColumnDescriptionsAsync(connection, tables, cancellationToken)
                .ConfigureAwait(false);

            schema.Tables = tables.Values
                .OrderBy(t => t.Schema, StringComparer.OrdinalIgnoreCase)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var views = await ReadViewsAsync(connection, cancellationToken)
                .ConfigureAwait(false);
            await PopulateViewColumnsAsync(connection, views, cancellationToken)
                .ConfigureAwait(false);
            await PopulateViewDescriptionsAsync(connection, views, cancellationToken)
                .ConfigureAwait(false);
            await PopulateViewColumnDescriptionsAsync(connection, views, cancellationToken)
                .ConfigureAwait(false);

            schema.Views = views.Values
                .OrderBy(v => v.Schema, StringComparer.OrdinalIgnoreCase)
                .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return schema;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"Failed to read schema from SQL Server: {ex.Message}",
                ex);
        }
    }

    private static async Task<string> ReadDatabaseNameAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.DatabaseName;

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result?.ToString() ?? string.Empty;
    }

    private static async Task<Dictionary<TableKey, TableInfo>> ReadTablesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var tables = new Dictionary<TableKey, TableInfo>();

        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.Tables;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var table = new TableInfo
            {
                Schema = reader.GetString(0),
                Name = reader.GetString(1),
            };

            tables[new TableKey(table.Schema, table.Name)] = table;
        }

        return tables;
    }

    private static async Task PopulateColumnsAsync(
        SqlConnection connection,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.Columns;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            var dataType = reader.GetString(3);
            table.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(2),
                DataType = dataType,
                IsNullable = reader.GetBoolean(4),
                MaxLength = NormalizeMaxLength(dataType, reader.GetInt16(5)),
            });
        }
    }

    private static async Task PopulatePrimaryKeysAsync(
        SqlConnection connection,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.PrimaryKeys;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            var columnName = reader.GetString(2);
            var column = table.Columns.FirstOrDefault(
                c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (column is not null)
            {
                column.IsPrimaryKey = true;
            }
        }
    }

    private static async Task PopulateForeignKeysAsync(
        SqlConnection connection,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.ForeignKeys;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            table.ForeignKeys.Add(new ForeignKeyInfo
            {
                Name = reader.GetString(2),
                Column = reader.GetString(3),
                ReferencedSchema = reader.GetString(4),
                ReferencedTable = reader.GetString(5),
                ReferencedColumn = reader.GetString(6),
            });
        }
    }

    private static async Task PopulateIndexesAsync(
        SqlConnection connection,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        var indexesByTable = new Dictionary<TableKey, Dictionary<string, IndexInfo>>();

        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.Indexes;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var tableKey = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!tables.ContainsKey(tableKey))
            {
                continue;
            }

            var indexName = reader.GetString(2);
            if (!indexesByTable.TryGetValue(tableKey, out var tableIndexes))
            {
                tableIndexes = new Dictionary<string, IndexInfo>(StringComparer.OrdinalIgnoreCase);
                indexesByTable[tableKey] = tableIndexes;
            }

            if (!tableIndexes.TryGetValue(indexName, out var index))
            {
                index = new IndexInfo
                {
                    Name = indexName,
                    IsUnique = reader.GetBoolean(3),
                };
                tableIndexes[indexName] = index;
            }

            index.Columns.Add(reader.GetString(4));
        }

        foreach (var (tableKey, tableIndexes) in indexesByTable)
        {
            if (!tables.TryGetValue(tableKey, out var table))
            {
                continue;
            }

            foreach (var index in tableIndexes.Values.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            {
                table.Indexes.Add(index);
            }
        }
    }

    private static async Task PopulateTableDescriptionsAsync(
        SqlConnection connection,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.TableDescriptions;
        command.Parameters.Add(new SqlParameter("@PropertyName", MsDescriptionProperty));

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            table.Description = reader.IsDBNull(2) ? null : reader.GetString(2);
        }
    }

    private static async Task PopulateColumnDescriptionsAsync(
        SqlConnection connection,
        Dictionary<TableKey, TableInfo> tables,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.ColumnDescriptions;
        command.Parameters.Add(new SqlParameter("@PropertyName", MsDescriptionProperty));

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!tables.TryGetValue(key, out var table))
            {
                continue;
            }

            var columnName = reader.GetString(2);
            var column = table.Columns.FirstOrDefault(
                c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (column is not null)
            {
                column.Description = reader.IsDBNull(3) ? null : reader.GetString(3);
            }
        }
    }

    private static async Task<Dictionary<TableKey, ViewInfo>> ReadViewsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var views = new Dictionary<TableKey, ViewInfo>();

        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.Views;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var view = new ViewInfo
            {
                Schema = reader.GetString(0),
                Name = reader.GetString(1),
                Definition = reader.IsDBNull(2) ? null : reader.GetString(2),
            };

            views[new TableKey(view.Schema, view.Name)] = view;
        }

        return views;
    }

    private static async Task PopulateViewColumnsAsync(
        SqlConnection connection,
        Dictionary<TableKey, ViewInfo> views,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.ViewColumns;

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!views.TryGetValue(key, out var view))
            {
                continue;
            }

            var dataType = reader.GetString(3);
            view.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(2),
                DataType = dataType,
                IsNullable = reader.GetBoolean(4),
                IsPrimaryKey = false,
                MaxLength = NormalizeMaxLength(dataType, reader.GetInt16(5)),
            });
        }
    }

    private static async Task PopulateViewDescriptionsAsync(
        SqlConnection connection,
        Dictionary<TableKey, ViewInfo> views,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.ViewDescriptions;
        command.Parameters.Add(new SqlParameter("@PropertyName", MsDescriptionProperty));

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!views.TryGetValue(key, out var view))
            {
                continue;
            }

            view.Description = reader.IsDBNull(2) ? null : reader.GetString(2);
        }
    }

    private static async Task PopulateViewColumnDescriptionsAsync(
        SqlConnection connection,
        Dictionary<TableKey, ViewInfo> views,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SqlServerSchemaQueries.ViewColumnDescriptions;
        command.Parameters.Add(new SqlParameter("@PropertyName", MsDescriptionProperty));

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!views.TryGetValue(key, out var view))
            {
                continue;
            }

            var columnName = reader.GetString(2);
            var column = view.Columns.FirstOrDefault(
                c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));

            if (column is not null)
            {
                column.Description = reader.IsDBNull(3) ? null : reader.GetString(3);
            }
        }
    }

    private static int? NormalizeMaxLength(string dataType, short maxLength)
    {
        if (maxLength < 0)
        {
            return null;
        }

        return dataType switch
        {
            "nchar" or "nvarchar" => maxLength / 2,
            "char" or "varchar" or "binary" or "varbinary" => maxLength,
            _ => null,
        };
    }

    private readonly record struct TableKey(string Schema, string Name);
}
