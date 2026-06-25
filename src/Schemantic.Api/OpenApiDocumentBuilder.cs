using System.Text.Json.Nodes;
using Schemantic.Core.Model;

namespace Schemantic.Api;

/// <summary>
/// Builds an OpenAPI 3.0 document from a <see cref="DatabaseSchema"/> without endpoint reflection.
/// </summary>
public static class OpenApiDocumentBuilder
{
    /// <summary>Produces an OpenAPI 3.0 JSON document for the schema's REST surface.</summary>
    /// <param name="schema">Introspected database schema used to generate paths and component schemas.</param>
    /// <returns>OpenAPI 3.0 document as a <see cref="JsonObject"/>.</returns>
    public static JsonObject Build(DatabaseSchema schema)
    {
        var paths = new JsonObject();

        paths["/schema"] = new JsonObject
        {
            ["get"] = new JsonObject
            {
                ["summary"] = "Database schema metadata",
                ["operationId"] = "getSchema",
                ["responses"] = OkResponse("DatabaseSchema", schemaComponentRef: "#/components/schemas/DatabaseSchema"),
            },
        };

        foreach (var table in schema.Tables.OrderBy(t => t.Schema).ThenBy(t => t.Name))
        {
            var listPath = $"/api/{table.Schema}/{table.Name}";
            var itemPath = $"{listPath}/{{id}}";
            var entitySchema = TableSchemaName(table);

            paths[listPath] = new JsonObject
            {
                ["get"] = new JsonObject
                {
                    ["summary"] = $"List rows in {table.Schema}.{table.Name}",
                    ["operationId"] = $"list_{table.Schema}_{table.Name}",
                    ["parameters"] = new JsonArray(PageParameter(), PageSizeParameter()),
                    ["responses"] = OkResponse($"Paged{entitySchema}", $"#/components/schemas/Paged{entitySchema}"),
                },
            };

            paths[itemPath] = new JsonObject
            {
                ["get"] = new JsonObject
                {
                    ["summary"] = $"Get row by primary key from {table.Schema}.{table.Name}",
                    ["operationId"] = $"get_{table.Schema}_{table.Name}_by_id",
                    ["parameters"] = new JsonArray(
                        new JsonObject
                        {
                            ["name"] = "id",
                            ["in"] = "path",
                            ["required"] = true,
                            ["schema"] = PrimaryKeySchema(table),
                        }),
                    ["responses"] = new JsonObject
                    {
                        ["200"] = new JsonObject
                        {
                            ["description"] = "Row found",
                            ["content"] = new JsonObject
                            {
                                ["application/json"] = new JsonObject
                                {
                                    ["schema"] = new JsonObject { ["$ref"] = $"#/components/schemas/{entitySchema}" },
                                },
                            },
                        },
                        ["404"] = NotFoundResponse(),
                    },
                },
            };
        }

        var components = new JsonObject
        {
            ["schemas"] = BuildSchemas(schema),
        };

        return new JsonObject
        {
            ["openapi"] = "3.0.3",
            ["info"] = new JsonObject
            {
                ["title"] = "Schemantic API",
                ["description"] = $"Read-only REST API for database '{schema.DatabaseName}'",
                ["version"] = "0.6.0",
            },
            ["paths"] = paths,
            ["components"] = components,
        };
    }

    private static JsonObject BuildSchemas(DatabaseSchema schema)
    {
        var schemas = new JsonObject
        {
            ["DatabaseSchema"] = new JsonObject
            {
                ["type"] = "object",
                ["description"] = "Introspected database schema metadata",
            },
        };

        foreach (var table in schema.Tables)
        {
            var entityName = TableSchemaName(table);
            var properties = new JsonObject();
            foreach (var column in table.Columns)
            {
                properties[column.Name] = ColumnOpenApiSchema(column);
            }

            schemas[entityName] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
            };

            schemas[$"Paged{entityName}"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["page"] = new JsonObject { ["type"] = "integer" },
                    ["pageSize"] = new JsonObject { ["type"] = "integer" },
                    ["items"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject { ["$ref"] = $"#/components/schemas/{entityName}" },
                    },
                },
            };
        }

        return schemas;
    }

    private static string TableSchemaName(TableInfo table) =>
        $"{Sanitize(table.Schema)}_{Sanitize(table.Name)}";

    private static string Sanitize(string value) =>
        new(value.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

    private static JsonObject ColumnOpenApiSchema(ColumnInfo column)
    {
        var type = column.DataType.ToUpperInvariant();
        if (type.Contains("INT", StringComparison.Ordinal))
        {
            return new JsonObject { ["type"] = "integer" };
        }

        if (type is "REAL" or "FLOAT" or "DOUBLE" or "NUMERIC" or "DECIMAL")
        {
            return new JsonObject { ["type"] = "number" };
        }

        if (type is "BLOB")
        {
            return new JsonObject { ["type"] = "string", ["format"] = "byte" };
        }

        return new JsonObject { ["type"] = "string" };
    }

    private static JsonObject PrimaryKeySchema(TableInfo table)
    {
        var pk = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);
        if (pk is null)
        {
            return new JsonObject { ["type"] = "string" };
        }

        return ColumnOpenApiSchema(pk);
    }

    private static JsonObject PageParameter() => new()
    {
        ["name"] = "page",
        ["in"] = "query",
        ["schema"] = new JsonObject { ["type"] = "integer", ["minimum"] = 1, ["default"] = 1 },
    };

    private static JsonObject PageSizeParameter() => new()
    {
        ["name"] = "pageSize",
        ["in"] = "query",
        ["schema"] = new JsonObject { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = 1000, ["default"] = 50 },
    };

    private static JsonObject OkResponse(string description, string schemaComponentRef) => new()
    {
        ["200"] = new JsonObject
        {
            ["description"] = description,
            ["content"] = new JsonObject
            {
                ["application/json"] = new JsonObject
                {
                    ["schema"] = new JsonObject { ["$ref"] = schemaComponentRef },
                },
            },
        },
    };

    private static JsonObject NotFoundResponse() => new()
    {
        ["description"] = "Table or row not found",
    };
}
