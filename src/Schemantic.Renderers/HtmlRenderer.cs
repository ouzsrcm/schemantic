using System.Text;
using Schemantic.Core.Abstractions;
using Schemantic.Core.Model;

namespace Schemantic.Renderers;

/// <summary>
/// Renders a <see cref="DatabaseSchema"/> as a single, self-contained HTML document
/// with client-side search, sidebar navigation, and a Mermaid entity-relationship
/// diagram generated from foreign keys.
/// </summary>
public sealed class HtmlRenderer : IRenderer
{
    private const string MermaidCdn = "https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js";

    /// <inheritdoc />
    public string FormatName => "html";

    /// <inheritdoc />
    public string Render(DatabaseSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        var tables = schema.Tables
            .OrderBy(t => t.Schema, StringComparer.Ordinal)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        var views = schema.Views
            .OrderBy(v => v.Schema, StringComparer.Ordinal)
            .ThenBy(v => v.Name, StringComparer.Ordinal)
            .ToList();

        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"<title>{HtmlEscape(schema.DatabaseName)} schema</title>");
        AppendStyles(sb);
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        AppendSidebar(sb, tables, views);

        sb.AppendLine("<main>");
        AppendHeader(sb, schema, tables.Count, views.Count);
        AppendDiagram(sb, tables);

        foreach (var table in tables)
        {
            AppendTable(sb, table);
        }

        if (views.Count > 0)
        {
            sb.AppendLine("<section class=\"group\"><h2>Views</h2></section>");
            foreach (var view in views)
            {
                AppendView(sb, view);
            }
        }

        sb.AppendLine("</main>");

        AppendScripts(sb);

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, DatabaseSchema schema, int tableCount, int viewCount)
    {
        sb.AppendLine("<header>");
        sb.AppendLine($"<h1>{HtmlEscape(schema.DatabaseName)}</h1>");
        sb.AppendLine($"<p class=\"meta\">Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC &middot; " +
                      $"Tables: {tableCount}" + (viewCount > 0 ? $" &middot; Views: {viewCount}" : string.Empty) + "</p>");
        sb.AppendLine("</header>");
    }

    private static void AppendSidebar(StringBuilder sb, IReadOnlyList<TableInfo> tables, IReadOnlyList<ViewInfo> views)
    {
        sb.AppendLine("<nav id=\"sidebar\">");
        sb.AppendLine("<input id=\"search\" type=\"search\" placeholder=\"Filter tables...\" autocomplete=\"off\">");
        sb.AppendLine("<ul id=\"nav-list\">");

        foreach (var table in tables)
        {
            var label = $"{table.Schema}.{table.Name}";
            var anchor = ToAnchor(table.Schema, table.Name);
            sb.AppendLine($"<li data-name=\"{HtmlEscape(label.ToLowerInvariant())}\">" +
                          $"<a href=\"#{anchor}\">{HtmlEscape(label)}</a></li>");
        }

        foreach (var view in views)
        {
            var label = $"{view.Schema}.{view.Name}";
            var anchor = ToAnchor(view.Schema, view.Name);
            sb.AppendLine($"<li class=\"view-item\" data-name=\"{HtmlEscape(label.ToLowerInvariant())}\">" +
                          $"<a href=\"#{anchor}\">{HtmlEscape(label)} <span class=\"tag\">view</span></a></li>");
        }

        sb.AppendLine("</ul>");
        sb.AppendLine("</nav>");
    }

    private static void AppendTable(StringBuilder sb, TableInfo table)
    {
        var label = $"{table.Schema}.{table.Name}";
        var anchor = ToAnchor(table.Schema, table.Name);

        sb.AppendLine($"<section class=\"entity\" id=\"{anchor}\" data-name=\"{HtmlEscape(label.ToLowerInvariant())}\">");
        sb.AppendLine($"<h2>{HtmlEscape(label)}</h2>");

        if (!string.IsNullOrWhiteSpace(table.Description))
        {
            sb.AppendLine($"<p class=\"desc\">{HtmlEscape(table.Description.Trim())}</p>");
        }

        if (!string.IsNullOrWhiteSpace(table.Interpretation))
        {
            sb.AppendLine($"<p class=\"ai\"><strong>AI summary:</strong> {HtmlEscape(table.Interpretation.Trim())}</p>");
        }

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Column</th><th>Type</th><th>Nullable</th><th>PK</th><th>Default</th><th>Description</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var column in table.Columns)
        {
            sb.Append("<tr><td>").Append(HtmlEscape(column.Name)).Append("</td>");
            sb.Append("<td>").Append(HtmlEscape(column.DataType)).Append("</td>");
            sb.Append("<td>").Append(column.IsNullable ? "Yes" : "No").Append("</td>");
            sb.Append("<td>").Append(column.IsPrimaryKey ? "&#10003;" : string.Empty).Append("</td>");
            sb.Append("<td>").Append(HtmlEscape(column.DefaultValue ?? string.Empty)).Append("</td>");
            sb.Append("<td>").Append(HtmlEscape(column.Description ?? string.Empty)).Append("</td></tr>");
            sb.AppendLine();
        }
        sb.AppendLine("</tbody></table>");

        var foreignKeys = table.ForeignKeys
            .OrderBy(fk => fk.Name, StringComparer.Ordinal)
            .ToList();

        if (foreignKeys.Count > 0)
        {
            sb.AppendLine("<h3>Foreign Keys</h3>");
            sb.AppendLine("<ul class=\"fk\">");
            foreach (var fk in foreignKeys)
            {
                var reference = $"{fk.ReferencedSchema}.{fk.ReferencedTable}.{fk.ReferencedColumn}";
                sb.AppendLine($"<li>{HtmlEscape(fk.Column)} &rarr; {HtmlEscape(reference)}</li>");
            }
            sb.AppendLine("</ul>");
        }

        var indexes = table.Indexes
            .OrderBy(index => index.Name, StringComparer.Ordinal)
            .ToList();

        if (indexes.Count > 0)
        {
            sb.AppendLine("<h3>Indexes</h3>");
            sb.AppendLine("<ul class=\"idx\">");
            foreach (var index in indexes)
            {
                var uniqueLabel = index.IsUnique ? "unique" : "non-unique";
                var columns = string.Join(", ", index.Columns);
                sb.AppendLine($"<li>{HtmlEscape(index.Name)} ({uniqueLabel}): {HtmlEscape(columns)}</li>");
            }
            sb.AppendLine("</ul>");
        }

        sb.AppendLine("</section>");
    }

    private static void AppendView(StringBuilder sb, ViewInfo view)
    {
        var label = $"{view.Schema}.{view.Name}";
        var anchor = ToAnchor(view.Schema, view.Name);

        sb.AppendLine($"<section class=\"entity view\" id=\"{anchor}\" data-name=\"{HtmlEscape(label.ToLowerInvariant())}\">");
        sb.AppendLine($"<h2>{HtmlEscape(label)} <span class=\"tag\">view</span></h2>");

        if (!string.IsNullOrWhiteSpace(view.Description))
        {
            sb.AppendLine($"<p class=\"desc\">{HtmlEscape(view.Description.Trim())}</p>");
        }

        sb.AppendLine("<table>");
        sb.AppendLine("<thead><tr><th>Column</th><th>Type</th><th>Nullable</th><th>Description</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var column in view.Columns)
        {
            sb.Append("<tr><td>").Append(HtmlEscape(column.Name)).Append("</td>");
            sb.Append("<td>").Append(HtmlEscape(column.DataType)).Append("</td>");
            sb.Append("<td>").Append(column.IsNullable ? "Yes" : "No").Append("</td>");
            sb.Append("<td>").Append(HtmlEscape(column.Description ?? string.Empty)).Append("</td></tr>");
            sb.AppendLine();
        }
        sb.AppendLine("</tbody></table>");

        if (!string.IsNullOrWhiteSpace(view.Definition))
        {
            sb.AppendLine("<details><summary>Definition</summary>");
            sb.AppendLine($"<pre><code>{HtmlEscape(view.Definition.Trim())}</code></pre>");
            sb.AppendLine("</details>");
        }

        sb.AppendLine("</section>");
    }

    /// <summary>
    /// Emits a Mermaid <c>erDiagram</c> block: one entity per table (with columns and
    /// key markers) plus a relationship line per foreign key that references a known table.
    /// </summary>
    private static void AppendDiagram(StringBuilder sb, IReadOnlyList<TableInfo> tables)
    {
        if (tables.Count == 0)
        {
            return;
        }

        var entityIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            entityIds[$"{table.Schema}.{table.Name}"] = ToEntityId(table.Schema, table.Name);
        }

        var diagram = new StringBuilder();
        diagram.AppendLine("erDiagram");

        foreach (var table in tables)
        {
            var entityId = entityIds[$"{table.Schema}.{table.Name}"];
            diagram.Append("  ").Append(entityId).AppendLine(" {");
            foreach (var column in table.Columns)
            {
                var type = ToMermaidToken(column.DataType);
                var name = ToMermaidToken(column.Name);
                var key = column.IsPrimaryKey ? " PK" : string.Empty;
                diagram.Append("    ").Append(type).Append(' ').Append(name).Append(key).AppendLine();
            }
            diagram.AppendLine("  }");
        }

        foreach (var table in tables)
        {
            var childId = entityIds[$"{table.Schema}.{table.Name}"];
            foreach (var fk in table.ForeignKeys.OrderBy(fk => fk.Name, StringComparer.Ordinal))
            {
                var referencedKey = $"{fk.ReferencedSchema}.{fk.ReferencedTable}";
                if (!entityIds.TryGetValue(referencedKey, out var parentId))
                {
                    continue;
                }

                var label = ToMermaidToken(string.IsNullOrWhiteSpace(fk.Column) ? fk.Name : fk.Column);
                diagram.Append("  ").Append(parentId).Append(" ||--o{ ").Append(childId)
                    .Append(" : ").AppendLine(label);
            }
        }

        sb.AppendLine("<section class=\"group\"><h2>Diagram</h2>");
        sb.AppendLine("<div class=\"mermaid\">");
        sb.Append(HtmlEscape(diagram.ToString()));
        sb.AppendLine("</div>");
        sb.AppendLine("</section>");
    }

    private static void AppendStyles(StringBuilder sb)
    {
        sb.AppendLine("<style>");
        sb.AppendLine(":root{--bg:#fff;--fg:#1b1f24;--muted:#6b7280;--border:#e5e7eb;--accent:#2563eb;--code:#f3f4f6;}");
        sb.AppendLine("*{box-sizing:border-box;}");
        sb.AppendLine("body{margin:0;font:15px/1.5 -apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:var(--fg);background:var(--bg);}");
        sb.AppendLine("#sidebar{position:fixed;top:0;left:0;width:260px;height:100vh;overflow:auto;border-right:1px solid var(--border);padding:16px;}");
        sb.AppendLine("#search{width:100%;padding:8px 10px;margin-bottom:12px;border:1px solid var(--border);border-radius:6px;font-size:14px;}");
        sb.AppendLine("#nav-list{list-style:none;margin:0;padding:0;}");
        sb.AppendLine("#nav-list li{margin:1px 0;}");
        sb.AppendLine("#nav-list a{display:block;padding:5px 8px;border-radius:5px;color:var(--fg);text-decoration:none;font-size:14px;}");
        sb.AppendLine("#nav-list a:hover{background:var(--code);}");
        sb.AppendLine("main{margin-left:260px;padding:24px 32px;max-width:1000px;}");
        sb.AppendLine("header h1{margin:0 0 4px;}");
        sb.AppendLine(".meta{color:var(--muted);margin:0 0 8px;}");
        sb.AppendLine(".entity{border-top:1px solid var(--border);padding-top:8px;margin-top:24px;scroll-margin-top:16px;}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;margin:8px 0;}");
        sb.AppendLine("th,td{border:1px solid var(--border);padding:6px 10px;text-align:left;vertical-align:top;}");
        sb.AppendLine("th{background:var(--code);font-weight:600;}");
        sb.AppendLine(".desc{color:var(--muted);}");
        sb.AppendLine(".ai{background:#eff6ff;border-left:3px solid var(--accent);padding:8px 12px;border-radius:0 6px 6px 0;}");
        sb.AppendLine(".tag{font-size:11px;background:var(--accent);color:#fff;border-radius:4px;padding:1px 6px;vertical-align:middle;}");
        sb.AppendLine("ul.fk,ul.idx{margin:4px 0 0;padding-left:20px;}");
        sb.AppendLine("pre{background:var(--code);padding:12px;border-radius:6px;overflow:auto;}");
        sb.AppendLine(".mermaid{background:var(--code);border-radius:6px;padding:12px;overflow:auto;}");
        sb.AppendLine("@media(max-width:800px){#sidebar{position:static;width:auto;height:auto;}main{margin-left:0;}}");
        sb.AppendLine("</style>");
    }

    private static void AppendScripts(StringBuilder sb)
    {
        sb.AppendLine($"<script src=\"{MermaidCdn}\"></script>");
        sb.AppendLine("<script>");
        sb.AppendLine("if(window.mermaid){mermaid.initialize({startOnLoad:true,securityLevel:'strict'});}");
        sb.AppendLine("(function(){");
        sb.AppendLine("  var box=document.getElementById('search');");
        sb.AppendLine("  if(!box)return;");
        sb.AppendLine("  var navItems=document.querySelectorAll('#nav-list li');");
        sb.AppendLine("  var sections=document.querySelectorAll('main section.entity');");
        sb.AppendLine("  box.addEventListener('input',function(){");
        sb.AppendLine("    var q=box.value.trim().toLowerCase();");
        sb.AppendLine("    navItems.forEach(function(li){");
        sb.AppendLine("      li.style.display=(!q||li.dataset.name.indexOf(q)>-1)?'':'none';");
        sb.AppendLine("    });");
        sb.AppendLine("    sections.forEach(function(s){");
        sb.AppendLine("      s.style.display=(!q||(s.dataset.name||'').indexOf(q)>-1)?'':'none';");
        sb.AppendLine("    });");
        sb.AppendLine("  });");
        sb.AppendLine("})();");
        sb.AppendLine("</script>");
    }

    /// <summary>Builds a GitHub-style heading anchor from schema and entity name.</summary>
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
        return result.Length > 0 ? result : "entity";
    }

    /// <summary>Builds a Mermaid-safe entity identifier (letters, digits, underscore).</summary>
    private static string ToEntityId(string schema, string name)
    {
        var token = ToMermaidToken($"{schema}_{name}");
        return token.Length > 0 ? token : "entity";
    }

    /// <summary>
    /// Reduces a string to a Mermaid-safe token (alphanumeric and underscore), so that
    /// type names like <c>nvarchar(50)</c> or qualified names do not break diagram syntax.
    /// </summary>
    private static string ToMermaidToken(string value)
    {
        var token = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            token.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        var result = token.ToString().Trim('_');
        return result.Length > 0 ? result : "x";
    }

    private static string HtmlEscape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
}
