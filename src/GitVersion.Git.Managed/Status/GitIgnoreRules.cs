using System.Text.RegularExpressions;

namespace GitVersion.Git;

/// <summary>
/// The parsed rules of a single ignore source (a <c>.gitignore</c> file or
/// <c>.git/info/exclude</c>), matched against paths relative to the source's directory.
/// </summary>
/// <seealso href="https://git-scm.com/docs/gitignore"/>
internal sealed class GitIgnoreRules
{
    private readonly List<(Regex Pattern, bool DirectoryOnly, bool Negated)> rules = [];

    private GitIgnoreRules(IEnumerable<string> lines, bool ignoreCase)
    {
        foreach (var line in lines)
        {
            if (ParseLine(line, ignoreCase) is { } rule)
            {
                this.rules.Add(rule);
            }
        }
    }

    /// <summary>
    /// Loads the rules from an ignore file, or returns <see langword="null"/> when the file does not exist.
    /// </summary>
    /// <param name="path">The path of the ignore file.</param>
    /// <param name="ignoreCase">Whether patterns match case-insensitively (<c>core.ignorecase</c>).</param>
    /// <returns>The parsed rules, if the file exists.</returns>
    public static GitIgnoreRules? Load(string path, bool ignoreCase) => File.Exists(path) ? new(File.ReadLines(path), ignoreCase) : null;

    /// <summary>
    /// Parses ignore rules from text lines.
    /// </summary>
    /// <param name="lines">The lines of an ignore file.</param>
    /// <param name="ignoreCase">Whether patterns match case-insensitively (<c>core.ignorecase</c>).</param>
    /// <returns>The parsed rules.</returns>
    public static GitIgnoreRules Parse(IEnumerable<string> lines, bool ignoreCase) => new(lines, ignoreCase);

    /// <summary>
    /// Determines whether this source decides the ignored state of the given path.
    /// The last matching rule wins, per gitignore semantics.
    /// </summary>
    /// <param name="relativePath">The path, relative to this source's directory, using forward slashes.</param>
    /// <param name="isDirectory">Whether the path refers to a directory.</param>
    /// <returns>
    /// <see langword="true"/> (ignored), <see langword="false"/> (explicitly re-included),
    /// or <see langword="null"/> when no rule matches.
    /// </returns>
    public bool? IsIgnored(string relativePath, bool isDirectory)
    {
        for (var i = this.rules.Count - 1; i >= 0; i--)
        {
            var (pattern, directoryOnly, negated) = this.rules[i];

            if (directoryOnly && !isDirectory)
            {
                continue;
            }

            if (pattern.IsMatch(relativePath))
            {
                return !negated;
            }
        }

        return null;
    }

    private static (Regex Pattern, bool DirectoryOnly, bool Negated)? ParseLine(string line, bool ignoreCase)
    {
        var pattern = TrimTrailingSpaces(line);

        if (pattern.Length == 0 || pattern[0] == '#')
        {
            return null;
        }

        var negated = pattern[0] == '!';

        if (negated)
        {
            pattern = pattern[1..];
        }

        var directoryOnly = pattern.EndsWith('/');

        if (directoryOnly)
        {
            pattern = pattern[..^1];
        }

        if (pattern.Length == 0)
        {
            return null;
        }

        // Patterns containing a slash are anchored to the directory of the ignore file,
        // while all other patterns match at any depth below it.
        if (!pattern.Contains('/'))
        {
            pattern = "**/" + pattern;
        }

        pattern = pattern.TrimStart('/');

        // git matches ignore patterns case-insensitively when core.ignorecase is set
        // (the default in repositories created on case-insensitive filesystems).
        var regexOptions = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        var regex = new Regex(
            "^" + TranslateToRegex(pattern) + "$",
            regexOptions,
            TimeSpan.FromSeconds(1));

        return (regex, directoryOnly, negated);
    }

    private static string TrimTrailingSpaces(string line)
    {
        var end = line.Length;

        while (end > 0 && line[end - 1] == ' ' && (end < 2 || line[end - 2] != '\\'))
        {
            end--;
        }

        return line[..end];
    }

    private static string TranslateToRegex(string pattern)
    {
        var regex = new StringBuilder();
        var i = 0;

        while (i < pattern.Length)
        {
            var atSegmentStart = i == 0 || pattern[i - 1] == '/';

            if (atSegmentStart && pattern.AsSpan(i).StartsWith("**/", StringComparison.Ordinal))
            {
                regex.Append("(?:[^/]+/)*");
                i += 3;
                continue;
            }

            if (atSegmentStart && pattern.AsSpan(i).SequenceEqual("**"))
            {
                regex.Append(".*");
                i += 2;
                continue;
            }

            var current = pattern[i];

            switch (current)
            {
                case '*' when i + 1 < pattern.Length && pattern[i + 1] == '*':
                    // Consecutive asterisks that are not at a path boundary act as
                    // regular asterisks, per the gitignore specification.
                    regex.Append("[^/]*");
                    i += 2;
                    break;

                case '*':
                    regex.Append("[^/]*");
                    i++;
                    break;

                case '?':
                    regex.Append("[^/]");
                    i++;
                    break;

                case '[':
                    i = AppendCharacterClass(pattern, i, regex);
                    break;

                case '\\' when i + 1 < pattern.Length:
                    regex.Append(Regex.Escape(pattern[i + 1].ToString()));
                    i += 2;
                    break;

                default:
                    regex.Append(Regex.Escape(current.ToString()));
                    i++;
                    break;
            }
        }

        return regex.ToString();
    }

    private static int AppendCharacterClass(string pattern, int start, StringBuilder regex)
    {
        var end = pattern.IndexOf(']', start + 1);

        if (end < 0)
        {
            // An unterminated class matches a literal bracket.
            regex.Append(Regex.Escape("["));
            return start + 1;
        }

        var body = pattern[(start + 1)..end];

        if (body.StartsWith('!'))
        {
            body = "^" + body[1..];
        }

        regex.Append('[').Append(body).Append(']');
        return end + 1;
    }
}
