using Schemantic.Core.Model;
using Schemantic.Providers.Sqlite;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for <see cref="SqliteSqlDialect"/> SQL generation.
/// </summary>
public class SqliteSqlDialectTests
{
    private readonly SqliteSqlDialect _dialect = new();

    [Fact]
    public void BuildSelectList_uses_limit_offset_and_whitelisted_columns()
    {
        var table = CreateAuthorTable();
        var sql = _dialect.BuildSelectList(table, page: 2, pageSize: 10);

        Assert.Contains("SELECT \"id\", \"full_name\"", sql.Sql, StringComparison.Ordinal);
        Assert.Contains("FROM \"author\"", sql.Sql, StringComparison.Ordinal);
        Assert.Contains("LIMIT @limit OFFSET @offset", sql.Sql, StringComparison.Ordinal);
        Assert.Equal(10, sql.Parameters.First(p => p.Name == "@limit").Value);
        Assert.Equal(10, sql.Parameters.First(p => p.Name == "@offset").Value);
    }

    [Fact]
    public void BuildSelectByPrimaryKey_binds_integer_id_parameter()
    {
        var table = CreateAuthorTable();
        var sql = _dialect.BuildSelectByPrimaryKey(table, "42");

        Assert.Contains("WHERE \"id\" = @id", sql.Sql, StringComparison.Ordinal);
        Assert.Equal(42L, sql.Parameters.Single(p => p.Name == "@id").Value);
    }

    [Fact]
    public void BuildSelectByPrimaryKey_rejects_composite_primary_key()
    {
        var table = CreateAuthorTable();
        table.Columns.Add(new ColumnInfo { Name = "tenant_id", DataType = "INTEGER", IsPrimaryKey = true });

        Assert.Throws<InvalidOperationException>(() => _dialect.BuildSelectByPrimaryKey(table, "1"));
    }

    private static TableInfo CreateAuthorTable() => new()
    {
        Schema = "main",
        Name = "author",
        Columns =
        [
            new ColumnInfo { Name = "id", DataType = "INTEGER", IsPrimaryKey = true },
            new ColumnInfo { Name = "full_name", DataType = "TEXT" },
        ],
    };
}
