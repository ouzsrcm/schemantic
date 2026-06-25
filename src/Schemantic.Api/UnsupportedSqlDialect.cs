using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Api;

/// <summary>
/// Placeholder dialect for providers whose query SQL is not yet implemented.
/// </summary>
internal sealed class UnsupportedSqlDialect : ISqlDialect
{
    private readonly string _engine;

    public UnsupportedSqlDialect(string engine) => _engine = engine;

    /// <inheritdoc />
    public ParameterizedSql BuildSelectList(TableInfo table, int page, int pageSize) =>
        throw new NotSupportedException($"{_engine} data queries are not yet supported. Use --provider sqlite for API v0.6.");

    /// <inheritdoc />
    public ParameterizedSql BuildSelectByPrimaryKey(TableInfo table, string idValue) =>
        throw new NotSupportedException($"{_engine} data queries are not yet supported. Use --provider sqlite for API v0.6.");
}
