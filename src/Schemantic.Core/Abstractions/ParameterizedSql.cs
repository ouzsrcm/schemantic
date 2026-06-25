namespace Schemantic.Core.Abstractions;

/// <summary>
/// A SQL statement with bound parameters. Identifier names in <see cref="Sql"/> are
/// produced only from trusted schema metadata; values are always passed as parameters.
/// </summary>
public sealed class ParameterizedSql
{
    /// <summary>Command text ready for <see cref="System.Data.Common.DbCommand.CommandText"/>.</summary>
    public string Sql { get; init; } = string.Empty;

    /// <summary>Bound parameter values keyed by provider-specific names (e.g. <c>@id</c>).</summary>
    public IReadOnlyList<SqlParameterValue> Parameters { get; init; } = Array.Empty<SqlParameterValue>();
}

/// <summary>A single named parameter value for a <see cref="ParameterizedSql"/>.</summary>
public sealed class SqlParameterValue
{
    /// <summary>Parameter name including prefix (e.g. <c>@pageSize</c>).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Parameter value; use <c>null</c> for SQL NULL.</summary>
    public object? Value { get; init; }
}
