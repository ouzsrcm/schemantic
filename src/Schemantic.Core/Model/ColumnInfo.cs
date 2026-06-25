namespace Schemantic.Core.Model;

/// <summary>
/// Metadata for a single column within a table.
/// </summary>
public class ColumnInfo
{
    /// <summary>Column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Provider-specific data type name (e.g. "nvarchar", "int").</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Whether the column accepts NULL values.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Whether the column participates in the primary key.</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>Maximum length for variable-length types, when applicable.</summary>
    public int? MaxLength { get; set; }

    /// <summary>Default value expression or literal, when defined.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Optional human-readable description from metadata.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional AI-generated summary describing the column. Populated only when an
    /// <see cref="Abstractions.IInterpreter"/> is run; null otherwise. Kept separate
    /// from <see cref="Description"/> so generated text never overwrites real metadata.
    /// </summary>
    public string? Interpretation { get; set; }
}
