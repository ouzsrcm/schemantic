using Schemantic.Core.Model;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for the shared schema model (DatabaseSchema and related types).
/// </summary>
public class SchemaModelTests
{
    [Fact]
    public void DatabaseSchema_Tables_is_initialized_as_empty_list()
    {
        var schema = new DatabaseSchema();

        Assert.NotNull(schema.Tables);
        Assert.Empty(schema.Tables);
        Assert.NotNull(schema.Views);
        Assert.Empty(schema.Views);
    }

    [Fact]
    public void ViewInfo_Columns_is_initialized_as_empty_list()
    {
        var view = new ViewInfo();

        Assert.NotNull(view.Columns);
        Assert.Empty(view.Columns);
    }

    [Fact]
    public void TableInfo_Collections_are_initialized_as_empty_lists()
    {
        var table = new TableInfo();

        Assert.NotNull(table.Columns);
        Assert.Empty(table.Columns);
        Assert.NotNull(table.ForeignKeys);
        Assert.Empty(table.ForeignKeys);
        Assert.NotNull(table.Indexes);
        Assert.Empty(table.Indexes);
    }

    [Fact]
    public void IndexInfo_Columns_is_initialized_as_empty_list()
    {
        var index = new IndexInfo();

        Assert.NotNull(index.Columns);
        Assert.Empty(index.Columns);
    }
}
