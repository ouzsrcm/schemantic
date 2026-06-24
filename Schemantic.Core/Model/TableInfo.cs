namespace Schemantic.Core.Model;

/// <summary>
/// Metadata for a single table or view within a database schema.
/// </summary>
public class TableInfo
{
    /// <summary>Schema namespace (e.g. "dbo").</summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>Table name without schema qualifier.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional human-readable description from metadata.</summary>
    public string? Description { get; set; }

    /// <summary>Columns defined on this table.</summary>
    public IList<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();

    /// <summary>Outgoing foreign key constraints.</summary>
    public IList<ForeignKeyInfo> ForeignKeys { get; set; } = new List<ForeignKeyInfo>();

    /// <summary>Indexes defined on this table.</summary>
    public IList<IndexInfo> Indexes { get; set; } = new List<IndexInfo>();
}
