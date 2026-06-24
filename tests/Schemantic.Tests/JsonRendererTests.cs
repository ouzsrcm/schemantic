using System.Text.Json;
using Schemantic.Core.Model;
using Schemantic.Renderers;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for <see cref="JsonRenderer"/>.
/// </summary>
public class JsonRendererTests
{
    [Fact]
    public void Render_produces_valid_json_containing_table_name()
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = "SampleDb",
            Tables =
            [
                new TableInfo
                {
                    Schema = "dbo",
                    Name = "Customers",
                    Columns =
                    [
                        new ColumnInfo
                        {
                            Name = "CustomerId",
                            DataType = "int",
                            IsNullable = false,
                            IsPrimaryKey = true,
                        },
                    ],
                },
            ],
            Views =
            [
                new ViewInfo
                {
                    Schema = "dbo",
                    Name = "ActiveCustomers",
                    Definition = "SELECT * FROM dbo.Customers",
                },
            ],
        };

        var renderer = new JsonRenderer();
        var output = renderer.Render(schema);

        using var document = JsonDocument.Parse(output);

        Assert.Contains("Customers", output);
        Assert.Equal("SampleDb", document.RootElement.GetProperty("databaseName").GetString());
        Assert.Equal("Customers", document.RootElement.GetProperty("tables")[0].GetProperty("name").GetString());
        Assert.Equal("ActiveCustomers", document.RootElement.GetProperty("views")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void Render_throws_when_schema_is_null()
    {
        var renderer = new JsonRenderer();

        Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));
    }
}
