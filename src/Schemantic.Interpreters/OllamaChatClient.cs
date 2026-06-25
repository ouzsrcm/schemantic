using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Schemantic.Interpreters;

/// <summary>
/// <see cref="IChatClient"/> for a local Ollama server, using its <c>/api/chat</c> endpoint
/// with streaming disabled.
/// </summary>
public sealed class OllamaChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _model;

    /// <summary>Creates a client targeting <paramref name="endpoint"/> (e.g. http://localhost:11434).</summary>
    public OllamaChatClient(HttpClient httpClient, string endpoint, string model)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = (endpoint ?? throw new ArgumentNullException(nameof(endpoint))).TrimEnd('/');
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <inheritdoc />
    public string Name => "Ollama";

    /// <inheritdoc />
    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest(
            _model,
            [new ChatMessage("system", systemPrompt), new ChatMessage("user", userPrompt)],
            Stream: false);

        using var response = await _httpClient
            .PostAsJsonAsync($"{_endpoint}/api/chat", request, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content
            .ReadFromJsonAsync<ChatResponse>(cancellationToken)
            .ConfigureAwait(false);

        return body?.Message?.Content ?? string.Empty;
    }

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatResponse(
        [property: JsonPropertyName("message")] ChatMessage? Message);
}
