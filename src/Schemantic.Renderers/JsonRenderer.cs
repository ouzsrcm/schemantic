using System.Text.Json;
using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Renderers;

/// <summary>
/// Renders a <see cref="DatabaseSchema"/> as indented JSON.
/// </summary>
public sealed class JsonRenderer : IRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <inheritdoc />
    public string FormatName => "json";

    /// <inheritdoc />
    public string Render(DatabaseSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return JsonSerializer.Serialize(schema, SerializerOptions);
    }
}
