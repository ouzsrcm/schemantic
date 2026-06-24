namespace Schemantic.Core.Model;

/// <summary>
/// Metadata for a foreign key constraint referencing another table.
/// </summary>
public class ForeignKeyInfo
{
    /// <summary>Constraint name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Local column that holds the foreign key.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Schema of the referenced table.</summary>
    public string ReferencedSchema { get; set; } = string.Empty;

    /// <summary>Name of the referenced table.</summary>
    public string ReferencedTable { get; set; } = string.Empty;

    /// <summary>Column in the referenced table.</summary>
    public string ReferencedColumn { get; set; } = string.Empty;
}
