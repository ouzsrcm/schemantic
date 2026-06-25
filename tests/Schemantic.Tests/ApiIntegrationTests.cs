using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Schemantic.Tests;

/// <summary>
/// Integration tests for <c>Schemantic.Api</c> using an on-disk SQLite database seeded from samples.
/// </summary>
public class ApiIntegrationTests : IClassFixture<ApiIntegrationTests.ApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_schema_returns_tables()
    {
        var response = await _client.GetAsync("/schema");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var tables = document.RootElement.GetProperty("tables");
        Assert.Equal(2, tables.GetArrayLength());
    }

    [Fact]
    public async Task Get_list_returns_paged_authors()
    {
        var response = await _client.GetAsync("/api/main/author?page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal(1, document.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(10, document.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, document.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Get_by_id_returns_single_author()
    {
        var response = await _client.GetAsync("/api/main/author/1");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal("Ahmet Yılmaz", document.RootElement.GetProperty("full_name").GetString());
    }

    [Fact]
    public async Task Get_by_id_returns_404_for_missing_row()
    {
        var response = await _client.GetAsync("/api/main/author/9999");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_list_returns_404_for_unknown_table()
    {
        var response = await _client.GetAsync("/api/main/missing?page=1&pageSize=10");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Openapi_document_lists_table_paths()
    {
        var response = await _client.GetAsync("/openapi.json");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/main/author", out _));
        Assert.True(paths.TryGetProperty("/api/main/book", out _));
    }

    /// <summary>WebApplicationFactory configured with a seeded SQLite database.</summary>
    public sealed class ApiFactory : WebApplicationFactory<Program>, IDisposable
    {
        public string DatabasePath { get; }

        public ApiFactory()
        {
            var (connectionString, path) = SqliteTestDatabase.CreateFromSeed();
            DatabasePath = path;
            ConnectionString = connectionString;
        }

        internal string ConnectionString { get; }

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseSetting("Schemantic:Provider", "sqlite");
            builder.UseSetting("Schemantic:Connection", ConnectionString);
        }

        void IDisposable.Dispose()
        {
            Dispose();
            if (File.Exists(DatabasePath))
            {
                try
                {
                    File.Delete(DatabasePath);
                }
                catch (IOException)
                {
                    // Best-effort cleanup on Windows when the file is still locked.
                }
            }
        }
    }
}

/// <summary>Creates temporary SQLite databases for tests.</summary>
internal static class SqliteTestDatabase
{
    public static (string ConnectionString, string DatabasePath) CreateFromSeed()
    {
        var repoRoot = FindRepoRoot();
        var seedPath = Path.Combine(repoRoot, "samples", "seed-sqlite.sql");
        var databasePath = Path.Combine(Path.GetTempPath(), $"schemantic-api-test-{Guid.NewGuid():N}.db");
        var seedSql = File.ReadAllText(seedPath);

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = seedSql;
        command.ExecuteNonQuery();

        return ($"Data Source={databasePath}", databasePath);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Schemantic.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (Schemantic.sln).");
    }
}
