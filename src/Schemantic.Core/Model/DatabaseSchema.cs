namespace Schemantic.Core.Model;

/// <summary>
/// Root container for a database schema independent of any specific database engine.
/// </summary>
public class DatabaseSchema
{
    /// <summary>Logical name of the database.</summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>Tables discovered in the database.</summary>
    public IList<TableInfo> Tables { get; set; } = new List<TableInfo>();
}
