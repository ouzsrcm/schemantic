using Schemantic.Core.Model;
using Schemantic.Interpreters;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for <see cref="LlmInterpreter"/> using a fake chat client (no network).
/// </summary>
public class LlmInterpreterTests
{
    private sealed class FakeChatClient : IChatClient
    {
        private readonly string _reply;

        public FakeChatClient(string reply) => _reply = reply;

        public string Name => "Fake";

        /// <summary>The user prompts captured across calls, for assertion.</summary>
        public List<string> UserPrompts { get; } = [];

        public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            UserPrompts.Add(userPrompt);
            return Task.FromResult(_reply);
        }
    }

    [Fact]
    public async Task InterpretAsync_populates_table_interpretation_for_each_table()
    {
        var schema = CreateSampleSchema();
        var client = new FakeChatClient("  Stores customer records.  ");
        var interpreter = new LlmInterpreter(client);

        var result = await interpreter.InterpretAsync(schema);

        Assert.Same(schema, result);
        Assert.Equal(2, client.UserPrompts.Count);
        Assert.All(result.Tables, t => Assert.Equal("Stores customer records.", t.Interpretation));
    }

    [Fact]
    public async Task InterpretAsync_sends_table_name_and_columns_in_prompt()
    {
        var schema = CreateSampleSchema();
        var client = new FakeChatClient("ok");

        await new LlmInterpreter(client).InterpretAsync(schema);

        var customersPrompt = client.UserPrompts[0];
        Assert.Contains("Table: dbo.Customers", customersPrompt);
        Assert.Contains("CustomerId", customersPrompt);
    }

    [Fact]
    public async Task InterpretAsync_leaves_interpretation_null_for_blank_reply()
    {
        var schema = CreateSampleSchema();
        var interpreter = new LlmInterpreter(new FakeChatClient("   "));

        await interpreter.InterpretAsync(schema);

        Assert.All(schema.Tables, t => Assert.Null(t.Interpretation));
    }

    [Fact]
    public void Constructor_throws_when_client_is_null()
    {
        Assert.Throws<ArgumentNullException>(() => new LlmInterpreter(null!));
    }

    [Fact]
    public async Task InterpretAsync_throws_when_schema_is_null()
    {
        var interpreter = new LlmInterpreter(new FakeChatClient("x"));

        await Assert.ThrowsAsync<ArgumentNullException>(() => interpreter.InterpretAsync(null!));
    }

    private static DatabaseSchema CreateSampleSchema() => new()
    {
        DatabaseName = "SampleDb",
        Tables =
        [
            new TableInfo
            {
                Schema = "dbo",
                Name = "Customers",
                Columns = [new ColumnInfo { Name = "CustomerId", DataType = "int", IsPrimaryKey = true }],
            },
            new TableInfo
            {
                Schema = "dbo",
                Name = "Orders",
                Columns = [new ColumnInfo { Name = "OrderId", DataType = "int", IsPrimaryKey = true }],
            },
        ],
    };
}
