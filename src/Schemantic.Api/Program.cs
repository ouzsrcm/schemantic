using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Schemantic.Api;
using Schemantic.Core.Model;

var builder = WebApplication.CreateBuilder(args);

ApiStartupOptions startupOptions;
try
{
    startupOptions = ApiStartupOptions.From(builder.Configuration, args);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Startup error: {ex.Message}");
    return 1;
}

ApiBootstrap.RuntimeContext runtime;
try
{
    runtime = await ApiBootstrap.LoadAsync(startupOptions).ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Startup error: {ex.Message}");
    return 1;
}

builder.Services.AddSingleton(runtime);
builder.Services.AddSingleton(runtime.Schema);
builder.Services.AddSingleton<DataQueryService>();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

var app = builder.Build();

app.MapGet("/schema", (DatabaseSchema schema) => Results.Json(schema, jsonOptions));

app.MapGet("/openapi.json", (DatabaseSchema schema) =>
{
    var document = OpenApiDocumentBuilder.Build(schema);
    return Results.Json(document, new JsonSerializerOptions { WriteIndented = true });
});

app.MapGet("/swagger", () => Results.Content(SwaggerUi.Html, "text/html"));
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/{schemaName}/{tableName}", async (
    string schemaName,
    string tableName,
    int? page,
    int? pageSize,
    DataQueryService queries,
    DatabaseSchema schema,
    ApiBootstrap.RuntimeContext context,
    CancellationToken cancellationToken) =>
{
    var table = TableResolver.FindTable(schema, schemaName, tableName);
    if (table is null)
    {
        return Results.NotFound(new { error = $"Table '{schemaName}.{tableName}' was not found." });
    }

    var resolvedPage = page is < 1 ? 1 : page ?? 1;
    var resolvedPageSize = pageSize ?? context.Options.DefaultPageSize;
    if (resolvedPageSize < 1)
    {
        return Results.BadRequest(new { error = "pageSize must be at least 1." });
    }

    if (resolvedPageSize > context.Options.MaxPageSize)
    {
        return Results.BadRequest(new { error = $"pageSize cannot exceed {context.Options.MaxPageSize}." });
    }

    try
    {
        var items = await queries.QueryListAsync(table, resolvedPage, resolvedPageSize, cancellationToken)
            .ConfigureAwait(false);

        return Results.Json(new PagedResult<Dictionary<string, object?>>
        {
            Page = resolvedPage,
            PageSize = resolvedPageSize,
            Items = items,
        }, jsonOptions);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/{schemaName}/{tableName}/{id}", async (
    string schemaName,
    string tableName,
    string id,
    DataQueryService queries,
    DatabaseSchema schema,
    CancellationToken cancellationToken) =>
{
    var table = TableResolver.FindTable(schema, schemaName, tableName);
    if (table is null)
    {
        return Results.NotFound(new { error = $"Table '{schemaName}.{tableName}' was not found." });
    }

    try
    {
        var row = await queries.QueryByIdAsync(table, id, cancellationToken).ConfigureAwait(false);
        return row is null
            ? Results.NotFound(new { error = $"Row with id '{id}' was not found." })
            : Results.Json(row, jsonOptions);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

Console.WriteLine($"Schemantic API — provider: {startupOptions.Provider}, tables: {runtime.Schema.Tables.Count}");
await app.RunAsync().ConfigureAwait(false);
return 0;

/// <summary>Entry point type for WebApplicationFactory integration tests.</summary>
public partial class Program;
