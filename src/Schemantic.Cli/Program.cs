using System.CommandLine;
using System.Diagnostics;
using Schemantic.Core.Abstractions;
using Schemantic.Interpreters;
using Schemantic.Providers.Oracle;
using Schemantic.Providers.Sqlite;
using Schemantic.Providers.SqlServer;
using Schemantic.Renderers;

var providers = new Dictionary<string, Func<string?, IDatabaseProvider>>(StringComparer.OrdinalIgnoreCase)
{
    ["sqlserver"] = _ => new SqlServerProvider(),
    ["oracle"] = schema => new OracleProvider(schema),
    ["sqlite"] = _ => new SqliteProvider(),
};

var renderers = new Dictionary<string, IRenderer>(StringComparer.OrdinalIgnoreCase)
{
    ["markdown"] = new MarkdownRenderer(),
    ["json"] = new JsonRenderer(),
    ["html"] = new HtmlRenderer(),
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

var outputOption = new Option<string?>("--output")
{
    Description = "Output file path (default: schema.md or schema.json based on --format).",
};

var schemaOption = new Option<string?>("--schema")
{
    Description = "Oracle schema owner to read (optional; defaults to the connected user).",
};

var interpretOption = new Option<bool>("--interpret")
{
    Description = "Add AI-generated table summaries via a local/remote LLM (opt-in).",
};

var llmProviderOption = new Option<string>("--llm-provider")
{
    Description = "LLM backend when --interpret is set: ollama | openai.",
    DefaultValueFactory = _ => "ollama",
};

var llmEndpointOption = new Option<string>("--llm-endpoint")
{
    Description = "LLM endpoint base URL.",
    DefaultValueFactory = _ => "http://localhost:11434",
};

var llmModelOption = new Option<string>("--llm-model")
{
    Description = "LLM model name.",
    DefaultValueFactory = _ => "qwen2.5-coder",
};

var llmApiKeyOption = new Option<string?>("--llm-api-key")
{
    Description = "API key for OpenAI-compatible endpoints (ignored by Ollama).",
};

rootCommand.Options.Add(providerOption);
rootCommand.Options.Add(connectionOption);
rootCommand.Options.Add(formatOption);
rootCommand.Options.Add(outputOption);
rootCommand.Options.Add(schemaOption);
rootCommand.Options.Add(interpretOption);
rootCommand.Options.Add(llmProviderOption);
rootCommand.Options.Add(llmEndpointOption);
rootCommand.Options.Add(llmModelOption);
rootCommand.Options.Add(llmApiKeyOption);

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var providerName = parseResult.GetValue(providerOption)!;
    var connectionString = parseResult.GetValue(connectionOption)!;
    var formatName = parseResult.GetValue(formatOption)!;
    var outputPath = parseResult.GetValue(outputOption) ?? GetDefaultOutputPath(formatName);
    var schemaOwner = parseResult.GetValue(schemaOption);
    var interpret = parseResult.GetValue(interpretOption);

    var stopwatch = Stopwatch.StartNew();

    try
    {
        if (!providers.TryGetValue(providerName, out var createProvider))
        {
            var available = string.Join(", ", providers.Keys);
            Console.Error.WriteLine($"Unknown provider '{providerName}'. Available providers: {available}.");
            return 1;
        }

        var provider = createProvider(schemaOwner);

        if (!renderers.TryGetValue(formatName, out var renderer))
        {
            var available = string.Join(", ", renderers.Keys);
            Console.Error.WriteLine($"Unknown format '{formatName}'. Available formats: {available}.");
            return 1;
        }

        var schema = await provider.ReadSchemaAsync(connectionString, cancellationToken).ConfigureAwait(false);

        if (interpret)
        {
            var llmOptions = new LlmOptions
            {
                Provider = parseResult.GetValue(llmProviderOption)!,
                Endpoint = parseResult.GetValue(llmEndpointOption)!,
                Model = parseResult.GetValue(llmModelOption)!,
                ApiKey = parseResult.GetValue(llmApiKeyOption),
            };

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var interpreter = InterpreterFactory.Create(llmOptions, httpClient);
            Console.WriteLine($"Interpreting schema with {interpreter.Name}...");
            await interpreter.InterpretAsync(schema, cancellationToken).ConfigureAwait(false);
        }

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

static string GetDefaultOutputPath(string formatName) => formatName.ToLowerInvariant() switch
{
    "json" => "schema.json",
    "html" => "schema.html",
    _ => "schema.md",
};
