using System.Text;
using System.Text.RegularExpressions;

namespace Schemantic.Core.Filtering;

/// <summary>
/// Minimal case-insensitive glob matcher supporting <c>*</c> (any run of characters) and
/// <c>?</c> (single character). Patterns are translated to anchored regular expressions.
/// </summary>
internal static class Glob
{
    /// <summary>Returns true if <paramref name="value"/> matches the glob <paramref name="pattern"/>.</summary>
    public static bool IsMatch(string pattern, string value)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        var regex = "^" + Translate(pattern) + "$";
        return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string Translate(string pattern)
    {
        var sb = new StringBuilder(pattern.Length * 2);
        foreach (var c in pattern)
        {
            switch (c)
            {
                case '*':
                    sb.Append(".*");
                    break;
                case '?':
                    sb.Append('.');
                    break;
                default:
                    sb.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        return sb.ToString();
    }
}
