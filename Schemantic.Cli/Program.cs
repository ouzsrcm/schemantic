using System.CommandLine;
using System.Diagnostics;
using Schemantic.Core.Abstractions;
using Schemantic.Providers.SqlServer;
using Schemantic.Renderers;

var providers = new Dictionary<string, IDatabaseProvider>(StringComparer.OrdinalIgnoreCase)
{
    ["sqlserver"] = new SqlServerProvider(),
};

var renderers = new Dictionary<string, IRenderer>(StringComparer.OrdinalIgnoreCase)
{
    ["markdown"] = new MarkdownRenderer(),
};

var rootCommand = new RootCommand("Reads database schema metadata and writes documentation.");

var providerOption = new Option<string>("--provider")
{
    Description = "Database provider to use.",
    DefaultValueFactory = _ => "sqlserver",
};

var connectionOption = new Option<string>("--connection")
{
    Description = "Connection string for the target database.",
    Required = true,
};

var formatOption = new Option<string>("--format")
{
    Description = "Output format.",
    DefaultValueFactory = _ => "markdown",
};

var outputOption = new Option<string>("--output")
{
    Description = "Output file path.",
    DefaultValueFactory = _ => "schema.md",
};

rootCommand.Options.Add(providerOption);
rootCommand.Options.Add(connectionOption);
rootCommand.Options.Add(formatOption);
rootCommand.Options.Add(outputOption);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var providerName = parseResult.GetValue(providerOption)!;
    var connectionString = parseResult.GetValue(connectionOption)!;
    var formatName = parseResult.GetValue(formatOption)!;
    var outputPath = parseResult.GetValue(outputOption)!;

    var stopwatch = Stopwatch.StartNew();

    try
    {
        if (!providers.TryGetValue(providerName, out var provider))
        {
            var available = string.Join(", ", providers.Keys);
            Console.Error.WriteLine($"Unknown provider '{providerName}'. Available providers: {available}.");
            return 1;
        }

        if (!renderers.TryGetValue(formatName, out var renderer))
        {
            var available = string.Join(", ", renderers.Keys);
            Console.Error.WriteLine($"Unknown format '{formatName}'. Available formats: {available}.");
            return 1;
        }

        var schema = await provider.ReadSchemaAsync(connectionString, cancellationToken).ConfigureAwait(false);
        var content = renderer.Render(schema);

        var fullOutputPath = Path.GetFullPath(outputPath);
        await File.WriteAllTextAsync(fullOutputPath, content, cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        Console.WriteLine($"Tables found: {schema.Tables.Count}");
        Console.WriteLine($"Output written to: {fullOutputPath}");
        Console.WriteLine($"Elapsed: {stopwatch.Elapsed.TotalSeconds:F2}s");

        return 0;
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
});

return await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);
