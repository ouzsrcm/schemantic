using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Providers.Sqlite;

/// <summary>
/// SQLite dialect: double-quoted identifiers and <c>LIMIT</c>/<c>OFFSET</c> paging.
/// </summary>
public sealed class SqliteSqlDialect : ISqlDialect
{
    /// <inheritdoc />
    public ParameterizedSql BuildSelectList(TableInfo table, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be at least 1.");
        }

        var columns = string.Join(", ", table.Columns.Select(c => QuoteIdentifier(c.Name)));
        var orderBy = BuildOrderBy(table);
        var offset = (page - 1) * pageSize;

        return new ParameterizedSql
        {
            Sql = $"SELECT {columns} FROM {QuoteTable(table)} ORDER BY {orderBy} LIMIT @limit OFFSET @offset",
            Parameters =
            [
                new SqlParameterValue { Name = "@limit", Value = pageSize },
                new SqlParameterValue { Name = "@offset", Value = offset },
            ],
        };
    }

    /// <inheritdoc />
    public ParameterizedSql BuildSelectByPrimaryKey(TableInfo table, string idValue)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(idValue);

        var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();
        if (pkColumns.Count != 1)
        {
            throw new InvalidOperationException(
                $"Table '{table.Schema}.{table.Name}' must have exactly one primary-key column for by-id queries.");
        }

        var pk = pkColumns[0];
        var columns = string.Join(", ", table.Columns.Select(c => QuoteIdentifier(c.Name)));

        return new ParameterizedSql
        {
            Sql =
                $"SELECT {columns} FROM {QuoteTable(table)} WHERE {QuoteIdentifier(pk.Name)} = @id LIMIT 1",
            Parameters =
            [
                new SqlParameterValue { Name = "@id", Value = ParseIdValue(pk, idValue) },
            ],
        };
    }

    private static string QuoteTable(TableInfo table)
    {
        if (string.IsNullOrEmpty(table.Schema) || string.Equals(table.Schema, "main", StringComparison.OrdinalIgnoreCase))
        {
            return QuoteIdentifier(table.Name);
        }

        return $"{QuoteIdentifier(table.Schema)}.{QuoteIdentifier(table.Name)}";
    }

    private static string BuildOrderBy(TableInfo table)
    {
        var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).OrderBy(c => c.Name).ToList();
        if (pkColumns.Count > 0)
        {
            return string.Join(", ", pkColumns.Select(c => QuoteIdentifier(c.Name)));
        }

        if (table.Columns.Count == 0)
        {
            throw new InvalidOperationException($"Table '{table.Schema}.{table.Name}' has no columns.");
        }

        return QuoteIdentifier(table.Columns[0].Name);
    }

    private static object ParseIdValue(ColumnInfo column, string idValue)
    {
        var type = column.DataType.ToUpperInvariant();
        if (type.Contains("INT", StringComparison.Ordinal))
        {
            if (!long.TryParse(idValue, out var integer))
            {
                throw new ArgumentException($"Value '{idValue}' is not a valid integer for column '{column.Name}'.");
            }

            return integer;
        }

        if (type is "REAL" or "FLOAT" or "DOUBLE" or "NUMERIC" or "DECIMAL")
        {
            if (!double.TryParse(idValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number))
            {
                throw new ArgumentException($"Value '{idValue}' is not a valid number for column '{column.Name}'.");
            }

            return number;
        }

        return idValue;
    }

    private static string QuoteIdentifier(string identifier) =>
        $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
