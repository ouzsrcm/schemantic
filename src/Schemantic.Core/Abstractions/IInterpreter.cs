using Schemantic.Core.Model;

namespace Schemantic.Core.Abstractions;

/// <summary>
/// Optional pipeline stage that enriches a <see cref="DatabaseSchema"/> with
/// AI-generated commentary (for example, per-table summaries). Interpreters fill
/// the <c>Interpretation</c> fields on the model and never modify metadata that a
/// provider read from the database. Running an interpreter is opt-in; without one
/// the tool behaves exactly as before.
/// </summary>
public interface IInterpreter
{
    /// <summary>
    /// Interpreter identifier (e.g. <c>"Ollama"</c>).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Enriches the given schema in place by populating <c>Interpretation</c> fields,
    /// then returns the same instance for convenience.
    /// </summary>
    /// <param name="schema">The schema to enrich.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The enriched <see cref="DatabaseSchema"/>.</returns>
    Task<DatabaseSchema> InterpretAsync(
        DatabaseSchema schema,
        CancellationToken cancellationToken = default);
}
