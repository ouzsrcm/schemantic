using Schemantic.Core.Abstractions;

namespace Schemantic.Interpreters;

/// <summary>
/// Builds an <see cref="IInterpreter"/> from <see cref="LlmOptions"/>, selecting the
/// matching <see cref="IChatClient"/> backend. This is the single place the CLI calls
/// to wire up the optional interpretation layer.
/// </summary>
public static class InterpreterFactory
{
    /// <summary>
    /// Creates an interpreter for the given options, using <paramref name="httpClient"/>
    /// for network calls.
    /// </summary>
    /// <exception cref="ArgumentException">The provider name is not recognized.</exception>
    public static IInterpreter Create(LlmOptions options, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpClient);

        IChatClient client = options.Provider.ToLowerInvariant() switch
        {
            "ollama" => new OllamaChatClient(httpClient, options.Endpoint, options.Model),
            "openai" => new OpenAiChatClient(httpClient, options.Endpoint, options.Model, options.ApiKey),
            _ => throw new ArgumentException(
                $"Unknown LLM provider '{options.Provider}'. Available: ollama, openai.",
                nameof(options)),
        };

        return new LlmInterpreter(client);
    }
}
