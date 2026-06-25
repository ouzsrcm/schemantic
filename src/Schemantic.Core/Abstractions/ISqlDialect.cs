using Schemantic.Core.Model;

namespace Schemantic.Core.Abstractions;

/// <summary>
/// Builds parameterized data-access SQL for a specific database engine. Table and column
/// identifiers are quoted from whitelisted <see cref="TableInfo"/> metadata only.
/// </summary>
public interface ISqlDialect
{
    /// <summary>
    /// Builds a paginated <c>SELECT</c> listing all columns for the given table.
    /// </summary>
    /// <param name="table">Resolved table metadata (whitelist source for identifiers).</param>
    /// <param name="page">One-based page number.</param>
    /// <param name="pageSize">Maximum rows per page (must be positive).</param>
    /// <returns>Parameterized SQL with offset/limit bindings.</returns>
    ParameterizedSql BuildSelectList(TableInfo table, int page, int pageSize);

    /// <summary>
    /// Builds a <c>SELECT</c> filtered by primary-key value. Composite keys are not supported in v0.6.
    /// </summary>
    /// <param name="table">Resolved table metadata (whitelist source for identifiers).</param>
    /// <param name="idValue">Primary-key value from the route (parsed per column type).</param>
    /// <returns>Parameterized SQL with an <c>@id</c> (or dialect-specific) binding.</returns>
    ParameterizedSql BuildSelectByPrimaryKey(TableInfo table, string idValue);
}
