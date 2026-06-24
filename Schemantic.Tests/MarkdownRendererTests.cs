using Schemantic.Core.Model;
using Schemantic.Renderers;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for <see cref="MarkdownRenderer"/>.
/// </summary>
public class MarkdownRendererTests
{
    [Fact]
    public void Render_includes_headers_table_rows_foreign_keys_and_descriptions()
    {
        var schema = CreateSampleSchema();
        var renderer = new MarkdownRenderer();

        var output = renderer.Render(schema);

        Assert.Contains("# SampleDb", output);
        Assert.Contains("Tables: 3", output);
        Assert.Contains("## Table of Contents", output);
        Assert.Contains("[dbo.Customers](#dbocustomers)", output);
        Assert.Contains("[dbo.Orders](#dboorders)", output);
        Assert.Contains("[dbo.Products](#dboproducts)", output);

        Assert.Contains("## dbo.Customers", output);
        Assert.Contains("Customer master records.", output);
        Assert.Contains("| Column | Type | Nullable | PK | Default | Description |", output);
        Assert.Contains("| CustomerId | int | No | ✓ |  | Primary key |", output);
        Assert.Contains("| Email | nvarchar(256) | No |  |  | Contact email |", output);

        Assert.Contains("## dbo.Orders", output);
        Assert.Contains("Sales orders placed by customers.", output);
        Assert.Contains("| OrderId | int | No | ✓ |  |  |", output);
        Assert.Contains("| CustomerId | int | No |  |  | References customer |", output);

        Assert.Contains("### Foreign Keys", output);
        Assert.Contains("- CustomerId -> dbo.Customers.CustomerId", output);

        Assert.Contains("### Indexes", output);
        Assert.Contains("- IX_Orders_CustomerId (non-unique): CustomerId", output);

        Assert.Contains("## dbo.Products", output);
        Assert.Contains("Product catalog entries.", output);
        Assert.Contains("| ProductId | int | No | ✓ |  |  |", output);
        Assert.Contains("| Name | nvarchar(200) | No |  |  | Display name |", output);
    }

    [Fact]
    public void Render_throws_when_schema_is_null()
    {
        var renderer = new MarkdownRenderer();

        Assert.Throws<ArgumentNullException>(() => renderer.Render(null!));
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
                            Description = "Primary key",
                        },
                        new ColumnInfo
                        {
                            Name = "Email",
                            DataType = "nvarchar(256)",
                            IsNullable = false,
                            Description = "Contact email",
                        },
                    ],
                },
                new TableInfo
                {
                    Schema = "dbo",
                    Name = "Orders",
                    Description = "Sales orders placed by customers.",
                    Columns =
                    [
                        new ColumnInfo
                        {
                            Name = "OrderId",
                            DataType = "int",
                            IsNullable = false,
                            IsPrimaryKey = true,
                        },
                        new ColumnInfo
                        {
                            Name = "CustomerId",
                            DataType = "int",
                            IsNullable = false,
                            Description = "References customer",
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
                    Indexes =
                    [
                        new IndexInfo
                        {
                            Name = "IX_Orders_CustomerId",
                            IsUnique = false,
                            Columns = ["CustomerId"],
                        },
                    ],
                },
                new TableInfo
                {
                    Schema = "dbo",
                    Name = "Products",
                    Description = "Product catalog entries.",
                    Columns =
                    [
                        new ColumnInfo
                        {
                            Name = "ProductId",
                            DataType = "int",
                            IsNullable = false,
                            IsPrimaryKey = true,
                        },
                        new ColumnInfo
                        {
                            Name = "Name",
                            DataType = "nvarchar(200)",
                            IsNullable = false,
                            Description = "Display name",
                        },
                    ],
                },
            ],
        };
    }
}
