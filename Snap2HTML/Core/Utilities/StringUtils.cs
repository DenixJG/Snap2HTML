using System.Text;
using System.Text.RegularExpressions;

namespace Snap2HTML.Core.Utilities;

/// <summary>
/// String utility methods for the application.
/// </summary>
public static class StringUtils
{
    /// <summary>
    /// Sorts a list of directories correctly even if they have spaces/periods in them.
    /// </summary>
    public static List<string> SortDirList(List<string> lstDirs)
    {
        for (var n = 0; n < lstDirs.Count; n++)
        {
            lstDirs[n] = lstDirs[n].Replace(" ", "1|1");
            lstDirs[n] = lstDirs[n].Replace(".", "2|2");
        }

        lstDirs.Sort();

        for (var n = 0; n < lstDirs.Count; n++)
        {
            lstDirs[n] = lstDirs[n].Replace("1|1", " ");
            lstDirs[n] = lstDirs[n].Replace("2|2", ".");
        }

        return lstDirs;
    }

    /// <summary>
    /// Replaces characters that may appear in filenames/paths that have special meaning to JavaScript.
    /// </summary>
    /// <remarks>
    /// Info on u2028/u2029: https://en.wikipedia.org/wiki/Newline#Unicode
    /// </remarks>
    public static string MakeCleanJsString(string s) =>
        s.Replace("\\", "\\\\")
         .Replace("&", "&amp;")
         .Replace("\u2028", "")
         .Replace("\u2029", "")
         .Replace("\u0004", "");

    /// <summary>
    /// Tests a string for matches against a wildcard pattern. Use ? and * as wildcards.
    /// </summary>
    public static bool IsWildcardMatch(string wildcard, string text, bool casesensitive)
    {
        var sb = new StringBuilder(wildcard.Length + 10);
        sb.Append('^');

        foreach (var c in wildcard)
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

        sb.Append('$');

        var options = casesensitive
            ? RegexOptions.None
            : RegexOptions.IgnoreCase;

        var regex = new Regex(sb.ToString(), options);
        return regex.IsMatch(text);
    }

    /// <summary>
    /// Converts a DateTime to Unix timestamp.
    /// </summary>
    public static int ToUnixTimestamp(DateTime value) =>
        (int)Math.Truncate((value.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds);

    /// <summary>
    /// Parses a string to long, returning 0 if parsing fails.
    /// </summary>
    public static long ParseLong(string s) =>
        long.TryParse(s, out var num) ? num : 0;
}
