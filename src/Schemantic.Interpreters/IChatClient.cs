namespace Schemantic.Interpreters;

/// <summary>
/// Low-level abstraction over a chat-style LLM endpoint. Implementations talk to a
/// specific backend (Ollama, OpenAI-compatible, ...) so that <see cref="LlmInterpreter"/>
/// stays independent of any particular provider. This is the pluggability seam for v0.5.
/// </summary>
public interface IChatClient
{
    /// <summary>Backend identifier (e.g. <c>"Ollama"</c>).</summary>
    string Name { get; }

    /// <summary>
    /// Sends a system + user prompt to the model and returns the completion text.
    /// </summary>
    /// <param name="systemPrompt">Instruction that sets the assistant's behaviour.</param>
    /// <param name="userPrompt">The concrete request (e.g. a serialized table).</param>
    /// <param name="cancellationToken">Token used to cancel the call.</param>
    /// <returns>The model's reply as plain text.</returns>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
