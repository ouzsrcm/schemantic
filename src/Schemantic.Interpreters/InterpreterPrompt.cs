using System.Text;
using Schemantic.Core.Model;

namespace Schemantic.Interpreters;

/// <summary>
/// Builds the prompts sent to the LLM. Kept separate and deterministic so the prompt
/// shape can be unit-tested without a live model.
/// </summary>
public static class InterpreterPrompt
{
    /// <summary>System instruction shared by all table interpretations.</summary>
    public const string System =
        "You are a database documentation assistant. Given a table's name and columns, " +
        "write a concise one or two sentence summary of what the table most likely " +
        "represents and stores. Reply with the summary text only — no preamble, no lists.";

    /// <summary>
    /// Serializes a table into a compact user prompt: schema-qualified name, any existing
    /// description, and one line per column (name, type, and key/nullability markers).
    /// </summary>
    public static string ForTable(TableInfo table)
    {
        ArgumentNullException.ThrowIfNull(table);

        var sb = new StringBuilder();
        sb.Append("Table: ").Append(table.Schema).Append('.').AppendLine(table.Name);

        if (!string.IsNullOrWhiteSpace(table.Description))
        {
            sb.Append("Existing description: ").AppendLine(table.Description.Trim());
        }

        sb.AppendLine("Columns:");
        foreach (var column in table.Columns)
        {
            sb.Append("- ").Append(column.Name).Append(' ').Append(column.DataType);
            if (column.IsPrimaryKey)
            {
                sb.Append(" [PK]");
            }

            if (!column.IsNullable)
            {
                sb.Append(" [NOT NULL]");
            }

            sb.AppendLine();
        }

        if (table.ForeignKeys.Count > 0)
        {
            sb.AppendLine("Foreign keys:");
            foreach (var fk in table.ForeignKeys)
            {
                sb.Append("- ").Append(fk.Column).Append(" -> ")
                    .Append(fk.ReferencedSchema).Append('.').Append(fk.ReferencedTable)
                    .Append('.').AppendLine(fk.ReferencedColumn);
            }
        }

        return sb.ToString();
    }
}
