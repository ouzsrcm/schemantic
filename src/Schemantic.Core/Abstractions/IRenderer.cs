using Schemantic.Core.Model;

namespace Schemantic.Core.Abstractions;

/// <summary>
/// Converts a <see cref="DatabaseSchema"/> into a string representation (e.g. Markdown).
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Output format identifier (e.g. <c>"markdown"</c>).
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Renders the given schema to a string in this renderer's output format.
    /// </summary>
    /// <param name="schema">The schema to render.</param>
    /// <returns>Formatted string output.</returns>
    string Render(DatabaseSchema schema);
}
