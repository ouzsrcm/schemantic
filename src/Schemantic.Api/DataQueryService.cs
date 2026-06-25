using System.Data.Common;
using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Api;

/// <summary>
/// Executes parameterized SELECT statements via the provider connection.
/// </summary>
public sealed class DataQueryService
{
    private readonly ApiBootstrap.RuntimeContext _context;

    /// <summary>Creates a query service bound to the startup runtime context.</summary>
    public DataQueryService(ApiBootstrap.RuntimeContext context) => _context = context;

    /// <summary>Runs a paginated list query for the given table.</summary>
    public async Task<IReadOnlyList<Dictionary<string, object?>>> QueryListAsync(
        TableInfo table,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var sql = _context.Dialect.BuildSelectList(table, page, pageSize);
        return await ExecuteQueryAsync(sql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs a by-primary-key query; returns null when no row matches.</summary>
    public async Task<Dictionary<string, object?>?> QueryByIdAsync(
        TableInfo table,
        string id,
        CancellationToken cancellationToken = default)
    {
        var sql = _context.Dialect.BuildSelectByPrimaryKey(table, id);
        var rows = await ExecuteQueryAsync(sql, cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : rows[0];
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> ExecuteQueryAsync(
        ParameterizedSql sql,
        CancellationToken cancellationToken)
    {
        await using var connection = _context.Provider.CreateConnection(_context.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql.Sql;
        BindParameters(command, sql.Parameters);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var rows = new List<Dictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    private static void BindParameters(DbCommand command, IReadOnlyList<SqlParameterValue> parameters)
    {
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }
    }
}
