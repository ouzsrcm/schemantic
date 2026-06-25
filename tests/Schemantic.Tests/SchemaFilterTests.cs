using Schemantic.Core.Filtering;
using Schemantic.Core.Model;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for <see cref="SchemaFilter"/> and its glob matching.
/// </summary>
public class SchemaFilterTests
{
    [Fact]
    public void Apply_with_empty_options_keeps_everything()
    {
        var schema = CreateSchema();

        SchemaFilter.Apply(schema, new SchemaFilterOptions());

        Assert.Equal(4, schema.Tables.Count);
        Assert.Single(schema.Views);
    }

    [Fact]
    public void Include_schemas_keeps_only_matching_schema()
    {
        var schema = CreateSchema();

        SchemaFilter.Apply(schema, new SchemaFilterOptions
        {
            Include = new FilterRule { Schemas = ["dbo"] },
        });

        Assert.All(schema.Tables, t => Assert.Equal("dbo", t.Schema));
        Assert.DoesNotContain(schema.Tables, t => t.Schema == "audit");
    }

    [Fact]
    public void Exclude_table_wildcard_removes_matching_tables()
    {
        var schema = CreateSchema();

        SchemaFilter.Apply(schema, new SchemaFilterOptions
        {
            Exclude = new FilterRule { Tables = ["*_tmp"] },
        });

        Assert.DoesNotContain(schema.Tables, t => t.Name == "Orders_tmp");
        Assert.Contains(schema.Tables, t => t.Name == "Orders");
    }

    [Fact]
    public void Exclude_wins_over_include()
    {
        var schema = CreateSchema();

        SchemaFilter.Apply(schema, new SchemaFilterOptions
        {
            Include = new FilterRule { Schemas = ["dbo"] },
            Exclude = new FilterRule { Tables = ["dbo.Orders"] },
        });

        Assert.DoesNotContain(schema.Tables, t => t.Name == "Orders");
        Assert.Contains(schema.Tables, t => t.Name == "Customers");
    }

    [Fact]
    public void Include_table_pattern_matches_qualified_name()
    {
        var schema = CreateSchema();

        SchemaFilter.Apply(schema, new SchemaFilterOptions
        {
            Include = new FilterRule { Tables = ["dbo.Cust*"] },
        });

        Assert.Single(schema.Tables);
        Assert.Equal("Customers", schema.Tables[0].Name);
    }

    [Fact]
    public void Apply_filters_views_too()
    {
        var schema = CreateSchema();

        SchemaFilter.Apply(schema, new SchemaFilterOptions
        {
            Exclude = new FilterRule { Schemas = ["dbo"] },
        });

        Assert.Empty(schema.Views);
    }

    [Fact]
    public void Apply_throws_on_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => SchemaFilter.Apply(null!, new SchemaFilterOptions()));
        Assert.Throws<ArgumentNullException>(() => SchemaFilter.Apply(new DatabaseSchema(), null!));
    }

    private static DatabaseSchema CreateSchema() => new()
    {
        DatabaseName = "SampleDb",
        Tables =
        [
            new TableInfo { Schema = "dbo", Name = "Customers" },
            new TableInfo { Schema = "dbo", Name = "Orders" },
            new TableInfo { Schema = "dbo", Name = "Orders_tmp" },
            new TableInfo { Schema = "audit", Name = "Log" },
        ],
        Views =
        [
            new ViewInfo { Schema = "dbo", Name = "ActiveCustomers" },
        ],
    };
}
