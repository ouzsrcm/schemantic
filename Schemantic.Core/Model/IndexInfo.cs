namespace Schemantic.Core.Model;

/// <summary>
/// Metadata for an index on a table.
/// </summary>
public class IndexInfo
{
    /// <summary>Index name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the index enforces uniqueness.</summary>
    public bool IsUnique { get; set; }

    /// <summary>Ordered list of columns included in the index.</summary>
    public IList<string> Columns { get; set; } = new List<string>();
}
