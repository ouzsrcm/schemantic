namespace Schemantic.Core.Model;

/// <summary>
/// Metadata for a single database view.
/// </summary>
public class ViewInfo
{
    /// <summary>Schema namespace (e.g. "dbo").</summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>View name without schema qualifier.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional human-readable description from metadata.</summary>
    public string? Description { get; set; }

    /// <summary>SQL definition of the view, when available from the provider.</summary>
    public string? Definition { get; set; }

    /// <summary>
    /// Optional AI-generated summary describing the view. Populated only when an
    /// <see cref="Abstractions.IInterpreter"/> is run; null otherwise.
    /// </summary>
    public string? Interpretation { get; set; }

    /// <summary>Columns exposed by this view.</summary>
    public IList<ColumnInfo> Columns { get; set; } = new List<ColumnInfo>();
}
