using Schemantic.Core.Model;

namespace Schemantic.Api;

/// <summary>
/// Resolves tables from an introspected <see cref="DatabaseSchema"/> (whitelist lookup).
/// </summary>
public static class TableResolver
{
    /// <summary>
    /// Finds a table by schema and name using case-insensitive comparison.
    /// </summary>
    public static TableInfo? FindTable(DatabaseSchema schema, string schemaName, string tableName) =>
        schema.Tables.FirstOrDefault(t =>
            string.Equals(t.Schema, schemaName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));
}
