namespace Schemantic.Providers.Oracle;

/// <summary>
/// Parameterized SQL statements used to read Oracle data dictionary metadata.
/// </summary>
internal static class OracleSchemaQueries
{
    internal const string CurrentUser = "SELECT USER FROM DUAL";

    internal const string DatabaseName = "SELECT SYS_CONTEXT('USERENV', 'DB_NAME') FROM DUAL";

    internal const string Tables = """
        SELECT TABLE_NAME
        FROM ALL_TABLES
        WHERE OWNER = :owner
        ORDER BY TABLE_NAME
        """;

    internal const string Columns = """
        SELECT
            TABLE_NAME,
            COLUMN_NAME,
            DATA_TYPE,
            DATA_LENGTH,
            DATA_PRECISION,
            DATA_SCALE,
            NULLABLE,
            DATA_DEFAULT,
            COLUMN_ID
        FROM ALL_TAB_COLUMNS
        WHERE OWNER = :owner
        ORDER BY TABLE_NAME, COLUMN_ID
        """;

    internal const string TableComments = """
        SELECT TABLE_NAME, COMMENTS
        FROM ALL_TAB_COMMENTS
        WHERE OWNER = :owner
        """;

    internal const string ColumnComments = """
        SELECT TABLE_NAME, COLUMN_NAME, COMMENTS
        FROM ALL_COL_COMMENTS
        WHERE OWNER = :owner
        """;

    internal const string PrimaryKeys = """
        SELECT
            c.TABLE_NAME,
            cc.COLUMN_NAME
        FROM ALL_CONSTRAINTS c
        INNER JOIN ALL_CONS_COLUMNS cc
            ON c.OWNER = cc.OWNER AND c.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
        WHERE c.OWNER = :owner
            AND c.CONSTRAINT_TYPE = 'P'
        ORDER BY c.TABLE_NAME, c.CONSTRAINT_NAME, cc.POSITION
        """;

    internal const string ForeignKeys = """
        SELECT
            fk.TABLE_NAME,
            fk.CONSTRAINT_NAME,
            fkc.COLUMN_NAME,
            pk.OWNER AS REFERENCED_SCHEMA,
            pk.TABLE_NAME AS REFERENCED_TABLE,
            pkc.COLUMN_NAME AS REFERENCED_COLUMN
        FROM ALL_CONSTRAINTS fk
        INNER JOIN ALL_CONS_COLUMNS fkc
            ON fk.OWNER = fkc.OWNER AND fk.CONSTRAINT_NAME = fkc.CONSTRAINT_NAME
        INNER JOIN ALL_CONSTRAINTS pk
            ON fk.R_OWNER = pk.OWNER AND fk.R_CONSTRAINT_NAME = pk.CONSTRAINT_NAME
        INNER JOIN ALL_CONS_COLUMNS pkc
            ON pk.OWNER = pkc.OWNER
            AND pk.CONSTRAINT_NAME = pkc.CONSTRAINT_NAME
            AND fkc.POSITION = pkc.POSITION
        WHERE fk.OWNER = :owner
            AND fk.CONSTRAINT_TYPE = 'R'
        ORDER BY fk.TABLE_NAME, fk.CONSTRAINT_NAME, fkc.POSITION
        """;

    internal const string Indexes = """
        SELECT
            i.TABLE_NAME,
            i.INDEX_NAME,
            i.UNIQUENESS,
            ic.COLUMN_NAME,
            ic.COLUMN_POSITION
        FROM ALL_INDEXES i
        INNER JOIN ALL_IND_COLUMNS ic
            ON i.OWNER = ic.INDEX_OWNER AND i.INDEX_NAME = ic.INDEX_NAME
        WHERE i.TABLE_OWNER = :owner
            AND NOT EXISTS (
                SELECT 1
                FROM ALL_CONSTRAINTS c
                WHERE c.OWNER = :owner
                    AND c.CONSTRAINT_TYPE = 'P'
                    AND c.TABLE_NAME = i.TABLE_NAME
                    AND c.CONSTRAINT_NAME = i.INDEX_NAME
            )
        ORDER BY i.TABLE_NAME, i.INDEX_NAME, ic.COLUMN_POSITION
        """;

    internal const string Views = """
        SELECT VIEW_NAME, TEXT
        FROM ALL_VIEWS
        WHERE OWNER = :owner
        ORDER BY VIEW_NAME
        """;

    internal const string ViewColumns = """
        SELECT
            TABLE_NAME,
            COLUMN_NAME,
            DATA_TYPE,
            DATA_LENGTH,
            DATA_PRECISION,
            DATA_SCALE,
            NULLABLE,
            COLUMN_ID
        FROM ALL_TAB_COLUMNS
        WHERE OWNER = :owner
            AND TABLE_NAME IN (SELECT VIEW_NAME FROM ALL_VIEWS WHERE OWNER = :owner)
        ORDER BY TABLE_NAME, COLUMN_ID
        """;

    internal const string ViewComments = """
        SELECT TABLE_NAME, COMMENTS
        FROM ALL_TAB_COMMENTS
        WHERE OWNER = :owner
            AND TABLE_TYPE = 'VIEW'
        """;
}
