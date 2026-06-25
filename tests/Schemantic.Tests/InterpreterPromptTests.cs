using Schemantic.Core.Model;
using Schemantic.Interpreters;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for <see cref="InterpreterPrompt"/>.
/// </summary>
public class InterpreterPromptTests
{
    [Fact]
    public void ForTable_includes_name_columns_keys_and_foreign_keys()
    {
        var table = new TableInfo
        {
            Schema = "dbo",
            Name = "Orders",
            Description = "Sales orders.",
            Columns =
            [
                new ColumnInfo { Name = "OrderId", DataType = "int", IsPrimaryKey = true, IsNullable = false },
                new ColumnInfo { Name = "CustomerId", DataType = "int", IsNullable = false },
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
        };

        var prompt = InterpreterPrompt.ForTable(table);

        Assert.Contains("Table: dbo.Orders", prompt);
        Assert.Contains("Existing description: Sales orders.", prompt);
        Assert.Contains("- OrderId int [PK] [NOT NULL]", prompt);
        Assert.Contains("- CustomerId int [NOT NULL]", prompt);
        Assert.Contains("- CustomerId -> dbo.Customers.CustomerId", prompt);
    }

    [Fact]
    public void ForTable_throws_when_table_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => InterpreterPrompt.ForTable(null!));
    }
}
