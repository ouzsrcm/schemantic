using Schemantic.Core.Model;

namespace Schemantic.Core.Filtering;

/// <summary>
/// Prunes a <see cref="DatabaseSchema"/> according to <see cref="SchemaFilterOptions"/>.
/// Applied after a provider reads metadata, so providers stay unaware of filtering and
/// every database engine gets the same include/exclude behaviour.
/// </summary>
public static class SchemaFilter
{
    /// <summary>
    /// Removes tables and views that the options exclude (or that an include allow-list
    /// leaves out), mutating and returning the same <paramref name="schema"/> instance.
    /// </summary>
    public static DatabaseSchema Apply(DatabaseSchema schema, SchemaFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(options);

        schema.Tables = schema.Tables
            .Where(t => Keep(t.Schema, t.Name, options))
            .ToList();

        schema.Views = schema.Views
            .Where(v => Keep(v.Schema, v.Name, options))
            .ToList();

        return schema;
    }

    private static bool Keep(string schema, string name, SchemaFilterOptions options)
    {
        if (options.Include is { } include)
        {
            if (include.Schemas.Count > 0 && !include.Schemas.Any(p => Glob.IsMatch(p, schema)))
            {
                return false;
            }

            if (include.Tables.Count > 0 && !include.Tables.Any(p => MatchesTable(p, schema, name)))
            {
                return false;
            }
        }

        if (options.Exclude is { } exclude)
        {
            if (exclude.Schemas.Any(p => Glob.IsMatch(p, schema)))
            {
                return false;
            }

            if (exclude.Tables.Any(p => MatchesTable(p, schema, name)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Matches a table pattern against both the bare and schema-qualified name.</summary>
    private static bool MatchesTable(string pattern, string schema, string name) =>
        Glob.IsMatch(pattern, name) || Glob.IsMatch(pattern, $"{schema}.{name}");
}
