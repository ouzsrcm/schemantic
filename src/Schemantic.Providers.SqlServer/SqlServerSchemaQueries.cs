namespace Schemantic.Providers.SqlServer;

/// <summary>
/// Parameterized SQL statements used to read SQL Server catalog metadata.
/// </summary>
internal static class SqlServerSchemaQueries
{
    /// <summary>Shared filter: user tables only, excluding system schemas.</summary>
    private const string UserTableFilter = """
        t.is_ms_shipped = 0
        AND s.name NOT IN (N'sys', N'INFORMATION_SCHEMA', N'guest')
        """;

    internal const string DatabaseName = "SELECT DB_NAME() AS DatabaseName;";

    internal static readonly string Tables = $"""
        SELECT
            s.name AS SchemaName,
            t.name AS TableName
        FROM sys.tables AS t
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        WHERE {UserTableFilter}
        ORDER BY s.name, t.name;
        """;

    internal static readonly string Columns = $"""
        SELECT
            s.name AS SchemaName,
            t.name AS TableName,
            c.name AS ColumnName,
            ty.name AS DataType,
            c.is_nullable AS IsNullable,
            c.max_length AS MaxLength
        FROM sys.columns AS c
        INNER JOIN sys.tables AS t ON c.object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        INNER JOIN sys.types AS ty ON c.user_type_id = ty.user_type_id
        WHERE {UserTableFilter}
        ORDER BY s.name, t.name, c.column_id;
        """;

    internal static readonly string PrimaryKeys = $"""
        SELECT
            s.name AS SchemaName,
            t.name AS TableName,
            col.name AS ColumnName
        FROM sys.indexes AS i
        INNER JOIN sys.index_columns AS ic
            ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns AS col
            ON ic.object_id = col.object_id AND ic.column_id = col.column_id
        INNER JOIN sys.tables AS t ON i.object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        WHERE i.is_primary_key = 1
            AND {UserTableFilter}
        ORDER BY s.name, t.name, ic.key_ordinal;
        """;

    internal static readonly string ForeignKeys = $"""
        SELECT
            s.name AS SchemaName,
            t.name AS TableName,
            fk.name AS ForeignKeyName,
            pc.name AS ColumnName,
            rs.name AS ReferencedSchema,
            rt.name AS ReferencedTable,
            rc.name AS ReferencedColumn
        FROM sys.foreign_keys AS fk
        INNER JOIN sys.foreign_key_columns AS fkc
            ON fk.object_id = fkc.constraint_object_id
        INNER JOIN sys.tables AS t ON fk.parent_object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        INNER JOIN sys.columns AS pc
            ON fkc.parent_object_id = pc.object_id
            AND fkc.parent_column_id = pc.column_id
        INNER JOIN sys.tables AS rt ON fk.referenced_object_id = rt.object_id
        INNER JOIN sys.schemas AS rs ON rt.schema_id = rs.schema_id
        INNER JOIN sys.columns AS rc
            ON fkc.referenced_object_id = rc.object_id
            AND fkc.referenced_column_id = rc.column_id
        WHERE {UserTableFilter}
        ORDER BY s.name, t.name, fk.name, fkc.constraint_column_id;
        """;

    internal static readonly string Indexes = $"""
        SELECT
            s.name AS SchemaName,
            t.name AS TableName,
            i.name AS IndexName,
            i.is_unique AS IsUnique,
            col.name AS ColumnName,
            ic.key_ordinal AS KeyOrdinal
        FROM sys.indexes AS i
        INNER JOIN sys.index_columns AS ic
            ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        INNER JOIN sys.columns AS col
            ON ic.object_id = col.object_id AND ic.column_id = col.column_id
        INNER JOIN sys.tables AS t ON i.object_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        WHERE i.is_primary_key = 0
            AND i.type > 0
            AND i.name IS NOT NULL
            AND {UserTableFilter}
        ORDER BY s.name, t.name, i.name, ic.key_ordinal;
        """;

    internal static readonly string TableDescriptions = $"""
        SELECT
            s.name AS SchemaName,
            t.name AS TableName,
            CAST(ep.value AS NVARCHAR(MAX)) AS Description
        FROM sys.extended_properties AS ep
        INNER JOIN sys.tables AS t ON ep.major_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        WHERE ep.name = @PropertyName
            AND ep.minor_id = 0
            AND {UserTableFilter};
        """;

    internal static readonly string ColumnDescriptions = $"""
        SELECT
            s.name AS SchemaName,
            t.name AS TableName,
            c.name AS ColumnName,
            CAST(ep.value AS NVARCHAR(MAX)) AS Description
        FROM sys.extended_properties AS ep
        INNER JOIN sys.tables AS t ON ep.major_id = t.object_id
        INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
        INNER JOIN sys.columns AS c
            ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
        WHERE ep.name = @PropertyName
            AND {UserTableFilter};
        """;

    /// <summary>Shared filter: user views only, excluding system schemas.</summary>
    private const string UserViewFilter = """
        v.is_ms_shipped = 0
        AND s.name NOT IN (N'sys', N'INFORMATION_SCHEMA', N'guest')
        """;

    internal static readonly string Views = $"""
        SELECT
            s.name AS SchemaName,
            v.name AS ViewName,
            m.definition AS Definition
        FROM sys.views AS v
        INNER JOIN sys.schemas AS s ON v.schema_id = s.schema_id
        LEFT JOIN sys.sql_modules AS m ON v.object_id = m.object_id
        WHERE {UserViewFilter}
        ORDER BY s.name, v.name;
        """;

    internal static readonly string ViewColumns = $"""
        SELECT
            s.name AS SchemaName,
            v.name AS ViewName,
            c.name AS ColumnName,
            ty.name AS DataType,
            c.is_nullable AS IsNullable,
            c.max_length AS MaxLength
        FROM sys.columns AS c
        INNER JOIN sys.views AS v ON c.object_id = v.object_id
        INNER JOIN sys.schemas AS s ON v.schema_id = s.schema_id
        INNER JOIN sys.types AS ty ON c.user_type_id = ty.user_type_id
        WHERE {UserViewFilter}
        ORDER BY s.name, v.name, c.column_id;
        """;

    internal static readonly string ViewDescriptions = $"""
        SELECT
            s.name AS SchemaName,
            v.name AS ViewName,
            CAST(ep.value AS NVARCHAR(MAX)) AS Description
        FROM sys.extended_properties AS ep
        INNER JOIN sys.views AS v ON ep.major_id = v.object_id
        INNER JOIN sys.schemas AS s ON v.schema_id = s.schema_id
        WHERE ep.name = @PropertyName
            AND ep.minor_id = 0
            AND {UserViewFilter};
        """;

    internal static readonly string ViewColumnDescriptions = $"""
        SELECT
            s.name AS SchemaName,
            v.name AS ViewName,
            c.name AS ColumnName,
            CAST(ep.value AS NVARCHAR(MAX)) AS Description
        FROM sys.extended_properties AS ep
        INNER JOIN sys.views AS v ON ep.major_id = v.object_id
        INNER JOIN sys.schemas AS s ON v.schema_id = s.schema_id
        INNER JOIN sys.columns AS c
            ON ep.major_id = c.object_id AND ep.minor_id = c.column_id
        WHERE ep.name = @PropertyName
            AND {UserViewFilter};
        """;
}
