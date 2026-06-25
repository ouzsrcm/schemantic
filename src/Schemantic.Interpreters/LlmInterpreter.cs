using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Interpreters;

/// <summary>
/// <see cref="IInterpreter"/> that produces a per-table summary by sending each table's
/// structure to an <see cref="IChatClient"/>. The chat client determines the backend
/// (Ollama, OpenAI-compatible, ...), so this class stays backend-agnostic.
/// </summary>
/// <remarks>
/// Skeleton scope (v0.5): table-level summaries only. Column- and view-level
/// interpretation can be added later by filling the corresponding model fields.
/// </remarks>
public sealed class LlmInterpreter : IInterpreter
{
    private readonly IChatClient _chatClient;

    /// <summary>Creates an interpreter that delegates completions to <paramref name="chatClient"/>.</summary>
    public LlmInterpreter(IChatClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <inheritdoc />
    public string Name => $"Llm({_chatClient.Name})";

    /// <inheritdoc />
    public async Task<DatabaseSchema> InterpretAsync(
        DatabaseSchema schema,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);

        foreach (var table in schema.Tables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userPrompt = InterpreterPrompt.ForTable(table);
            var summary = await _chatClient
                .CompleteAsync(InterpreterPrompt.System, userPrompt, cancellationToken)
                .ConfigureAwait(false);

            table.Interpretation = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        }

        return schema;
    }
}
