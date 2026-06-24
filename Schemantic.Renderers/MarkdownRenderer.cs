using System.Text;
using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Renderers;

/// <summary>
/// Renders a <see cref="DatabaseSchema"/> as Markdown documentation.
/// </summary>
public sealed class MarkdownRenderer : IRenderer
{
    /// <inheritdoc />
    public string FormatName => "markdown";

    /// <inheritdoc />
    public string Render(DatabaseSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var tables = schema.Tables
            .OrderBy(t => t.Schema, StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();

        sb.AppendLine($"# {schema.DatabaseName}");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Tables: {tables.Count}");
        sb.AppendLine();

        if (tables.Count > 0)
        {
            sb.AppendLine("## Table of Contents");
            sb.AppendLine();

            foreach (var table in tables)
            {
                var label = $"{table.Schema}.{table.Name}";
                var anchor = ToAnchor(table.Schema, table.Name);
                sb.AppendLine($"- [{label}](#{anchor})");
            }

            sb.AppendLine();
        }

        foreach (var table in tables)
        {
            RenderTable(sb, table);
        }

        return sb.ToString();
    }

    private static void RenderTable(StringBuilder sb, TableInfo table)
    {
        sb.AppendLine($"## {table.Schema}.{table.Name}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(table.Description))
        {
            sb.AppendLine(table.Description.Trim());
            sb.AppendLine();
        }

        sb.AppendLine("| Column | Type | Nullable | PK | Default | Description |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var column in table.Columns)
        {
            sb.Append('|').Append(' ').Append(EscapeTableCell(column.Name));
            sb.Append(" | ").Append(EscapeTableCell(column.DataType));
            sb.Append(" | ").Append(column.IsNullable ? "Yes" : "No");
            sb.Append(" | ").Append(column.IsPrimaryKey ? "✓" : "");
            sb.Append(" | ").Append(EscapeTableCell(column.DefaultValue ?? string.Empty));
            sb.Append(" | ").Append(EscapeTableCell(column.Description ?? string.Empty));
            sb.AppendLine(" |");
        }

        sb.AppendLine();

        var foreignKeys = table.ForeignKeys
            .OrderBy(fk => fk.Name, StringComparer.Ordinal)
            .ToList();

        if (foreignKeys.Count > 0)
        {
            sb.AppendLine("### Foreign Keys");
            sb.AppendLine();

            foreach (var fk in foreignKeys)
            {
                var reference = $"{fk.ReferencedSchema}.{fk.ReferencedTable}.{fk.ReferencedColumn}";
                sb.AppendLine($"- {fk.Column} -> {reference}");
            }

            sb.AppendLine();
        }

        var indexes = table.Indexes
            .OrderBy(index => index.Name, StringComparer.Ordinal)
            .ToList();

        if (indexes.Count > 0)
        {
            sb.AppendLine("### Indexes");
            sb.AppendLine();

            foreach (var index in indexes)
            {
                var uniqueLabel = index.IsUnique ? "unique" : "non-unique";
                var columns = string.Join(", ", index.Columns);
                sb.AppendLine($"- {index.Name} ({uniqueLabel}): {columns}");
            }

            sb.AppendLine();
        }
    }

    /// <summary>
    /// Builds a GitHub-flavored Markdown heading anchor from schema and table name.
    /// </summary>
    private static string ToAnchor(string schema, string name)
    {
        var text = $"{schema}.{name}".ToLowerInvariant();
        var anchor = new StringBuilder(text.Length);
        var lastWasHyphen = false;

        foreach (var character in text)
        {
            if (char.IsLetterOrDigit(character))
            {
                anchor.Append(character);
                lastWasHyphen = false;
            }
            else if (character is ' ' or '-')
            {
                if (!lastWasHyphen && anchor.Length > 0)
                {
                    anchor.Append('-');
                    lastWasHyphen = true;
                }
            }
        }

        var result = anchor.ToString().Trim('-');
        return result.Length > 0 ? result : "table";
    }

    private static string EscapeTableCell(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');
}
