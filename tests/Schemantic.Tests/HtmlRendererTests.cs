using Schemantic.Core.Model;
using Schemantic.Renderers;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for <see cref="HtmlRenderer"/>.
/// </summary>
public class HtmlRendererTests
{
    [Fact]
    public void Render_produces_self_contained_html_with_search_and_nav()
    {
        var schema = CreateSampleSchema();

        var output = new HtmlRenderer().Render(schema);

        Assert.StartsWith("<!DOCTYPE html>", output);
        Assert.Contains("<title>SampleDb schema</title>", output);
        Assert.Contains("id=\"search\"", output);
        Assert.Contains("id=\"nav-list\"", output);
        Assert.Contains("<h1>SampleDb</h1>", output);
        Assert.Contains("Tables: 2", output);
        Assert.EndsWith("</html>\n", output.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Render_includes_entity_sections_and_anchors()
    {
        var schema = CreateSampleSchema();

        var output = new HtmlRenderer().Render(schema);

        Assert.Contains("id=\"dbocustomers\"", output);
        Assert.Contains("id=\"dboorders\"", output);
        Assert.Contains("href=\"#dbocustomers\"", output);
        Assert.Contains("<h2>dbo.Customers</h2>", output);
        Assert.Contains("Foreign Keys", output);
        Assert.Contains("CustomerId &rarr; dbo.Customers.CustomerId", output);
    }

    [Fact]
    public void Render_emits_mermaid_er_diagram_from_foreign_keys()
    {
        var schema = CreateSampleSchema();

        var output = new HtmlRenderer().Render(schema);

        Assert.Contains("class=\"mermaid\"", output);
        Assert.Contains("erDiagram", output);
        // Entities exist for both tables.
        Assert.Contains("dbo_Customers {", output);
        Assert.Contains("dbo_Orders {", output);
        // FK becomes a parent ||--o{ child relationship labelled by the FK column.
        Assert.Contains("dbo_Customers ||--o{ dbo_Orders : CustomerId", output);
        // Mermaid CDN script is referenced.
        Assert.Contains("mermaid", output);
    }

    [Fact]
    public void Render_escapes_html_special_characters()
    {
        var schema = new DatabaseSchema
        {
            DatabaseName = "A&B",
            Tables =
            [
                new TableInfo
                {
                    Schema = "dbo",
                    Name = "T",
                    Description = "<script>alert(1)</script>",
                    Columns = [new ColumnInfo { Name = "c", DataType = "int" }],
                },
            ],
        };

        var output = new HtmlRenderer().Render(schema);

        Assert.Contains("A&amp;B", output);
        Assert.Contains("&lt;script&gt;", output);
        Assert.DoesNotContain("<script>alert(1)</script>", output);
    }

    [Fact]
    public void Render_throws_when_schema_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => new HtmlRenderer().Render(null!));
    }

    private static DatabaseSchema CreateSampleSchema()
    {
        return new DatabaseSchema
        {
            DatabaseName = "SampleDb",
            Tables =
            [
                new TableInfo
                {
                    Schema = "dbo",
                    Name = "Customers",
                    Description = "Customer master records.",
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
                new TableInfo
                {
                    Schema = "dbo",
                    Name = "Orders",
                    Columns =
                    [
                        new ColumnInfo
                        {
                            Name = "OrderId",
                            DataType = "int",
                            IsPrimaryKey = true,
                        },
                        new ColumnInfo
                        {
                            Name = "CustomerId",
                            DataType = "int",
                        },
                    ],
                    ForeignKeys =
                    [
                        new ForeignKeyInfo
                        {
                            Name = "FK_Orders_Customers",
                            Column = "CustomerId",
                            ReferencedSchema = "dbo",
                            ReferencedTable = "Customers",
                            ReferencedColumn = "CustomerId",
                        },
                    ],
                },
            ],
        };
    }
}
