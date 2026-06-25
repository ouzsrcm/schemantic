namespace Schemantic.Core.Filtering;

/// <summary>
/// Include/exclude rules that decide which schemas and tables/views appear in the output.
/// Typically deserialized from a JSON config file. All lists are optional; an empty
/// <see cref="SchemaFilterOptions"/> keeps everything.
/// </summary>
public sealed class SchemaFilterOptions
{
    /// <summary>When set, only matching objects are kept (allow-list). Empty lists mean "no restriction".</summary>
    public FilterRule? Include { get; set; }

    /// <summary>When set, matching objects are removed (deny-list). Exclude wins over include.</summary>
    public FilterRule? Exclude { get; set; }
}

/// <summary>
/// A set of schema and table/view name patterns. Patterns support <c>*</c> (any run of
/// characters) and <c>?</c> (single character) and are matched case-insensitively. Table
/// patterns are tested against both the bare name (e.g. <c>Orders</c>) and the
/// schema-qualified name (e.g. <c>dbo.Orders</c>).
/// </summary>
public sealed class FilterRule
{
    /// <summary>Schema name patterns (e.g. <c>dbo</c>, <c>audit_*</c>).</summary>
    public IList<string> Schemas { get; set; } = new List<string>();

    /// <summary>Table/view name patterns (e.g. <c>Orders</c>, <c>*_tmp</c>, <c>dbo.Stg*</c>).</summary>
    public IList<string> Tables { get; set; } = new List<string>();
}
