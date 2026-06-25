using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Schemantic.Interpreters;

/// <summary>
/// <see cref="IChatClient"/> for any OpenAI-compatible endpoint, using
/// <c>/v1/chat/completions</c> with streaming disabled. Works with OpenAI itself and with
/// compatible local servers (LM Studio, vLLM, etc.).
/// </summary>
public sealed class OpenAiChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _model;
    private readonly string? _apiKey;

    /// <summary>Creates a client targeting <paramref name="endpoint"/> (e.g. https://api.openai.com).</summary>
    public OpenAiChatClient(HttpClient httpClient, string endpoint, string model, string? apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = (endpoint ?? throw new ArgumentNullException(nameof(endpoint))).TrimEnd('/');
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _apiKey = apiKey;
    }

    /// <inheritdoc />
    public string Name => "OpenAI";

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

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/v1/chat/completions")
        {
            Content = JsonContent.Create(request),
        };

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        using var response = await _httpClient
            .SendAsync(message, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content
            .ReadFromJsonAsync<ChatResponse>(cancellationToken)
            .ConfigureAwait(false);

        return body?.Choices is { Count: > 0 } choices
            ? choices[0].Message?.Content ?? string.Empty
            : string.Empty;
    }

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<Choice>? Choices);

    private sealed record Choice(
        [property: JsonPropertyName("message")] ChatMessage? Message);
}
