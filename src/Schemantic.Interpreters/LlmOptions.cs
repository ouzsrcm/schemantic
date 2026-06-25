namespace Schemantic.Interpreters;

/// <summary>
/// Configuration for the LLM interpretation layer, sourced from CLI options.
/// </summary>
public sealed class LlmOptions
{
    /// <summary>Backend to use: <c>"ollama"</c> or <c>"openai"</c> (OpenAI-compatible).</summary>
    public string Provider { get; init; } = "ollama";

    /// <summary>Base URL of the LLM endpoint.</summary>
    public string Endpoint { get; init; } = "http://localhost:11434";

    /// <summary>Model name to request (e.g. <c>"qwen2.5-coder"</c>).</summary>
    public string Model { get; init; } = "qwen2.5-coder";

    /// <summary>API key for OpenAI-compatible endpoints; ignored by Ollama.</summary>
    public string? ApiKey { get; init; }
}
