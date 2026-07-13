namespace GitVersion.Git;

/// <summary>
/// A minimal parser for the repository-local <c>.git/config</c> file, covering the entries
/// GitVersion needs: <c>[remote "name"]</c> (url/fetch/push) and <c>[branch "name"]</c>
/// (remote/merge). Section names and keys are case-insensitive, subsection names are
/// case-sensitive, and later values override earlier ones (git's last-one-wins semantics).
/// </summary>
internal sealed class GitConfigurationFile
{
    private readonly List<(string Section, string? Subsection, string Key, string Value)> entries;

    private GitConfigurationFile(List<(string Section, string? Subsection, string Key, string Value)> entries) => this.entries = entries;

    /// <summary>
    /// Loads a configuration file, returning an empty configuration when the file does not exist.
    /// </summary>
    /// <param name="path">The path of the configuration file.</param>
    /// <returns>The parsed configuration.</returns>
    public static GitConfigurationFile Load(string path)
    {
        var entries = new List<(string, string?, string, string)>();

        if (!File.Exists(path))
        {
            return new(entries);
        }

        string? section = null;
        string? subsection = null;

        foreach (var rawLine in File.ReadLines(path, Encoding.UTF8))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('['))
            {
                (section, subsection) = ParseSectionHeader(line);
                continue;
            }

            if (section is null)
            {
                continue;
            }

            var separator = line.IndexOf('=');
            string key;
            string value;

            if (separator < 0)
            {
                // A key without '=' is a boolean true.
                key = StripComment(line).Trim();
                value = "true";
            }
            else
            {
                key = line[..separator].Trim();
                value = ParseValue(line[(separator + 1)..]);
            }

            if (key.Length > 0)
            {
                entries.Add((section, subsection, key.ToLowerInvariant(), value));
            }
        }

        return new(entries);
    }

    /// <summary>
    /// Gets the effective (last) value of a key, or <see langword="null"/> when not present.
    /// </summary>
    public string? GetString(string section, string? subsection, string key)
    {
        var values = GetAll(section, subsection, key);
        return values.Count > 0 ? values[^1] : null;
    }

    /// <summary>
    /// Gets all values of a multi-valued key, in file order.
    /// </summary>
    public IReadOnlyList<string> GetAll(string section, string? subsection, string key) =>
        [.. this.entries
            .Where(entry => entry.Section.Equals(section, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(entry.Subsection, subsection, StringComparison.Ordinal)
                            && entry.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Value)];

    /// <summary>
    /// Gets the distinct subsection names of a section, in file order.
    /// </summary>
    public IReadOnlyList<string> GetSubsections(string section) =>
        [.. this.entries
            .Where(entry => entry.Section.Equals(section, StringComparison.OrdinalIgnoreCase) && entry.Subsection is not null)
            .Select(entry => entry.Subsection!)
            .Distinct(StringComparer.Ordinal)];

    private static (string Section, string? Subsection) ParseSectionHeader(string line)
    {
        var end = line.IndexOf(']');
        var header = end > 0 ? line[1..end] : line[1..];
        var space = header.IndexOf(' ');

        if (space < 0)
        {
            // Handle the deprecated [section.subsection] form as a plain section name.
            return (header.Trim().ToLowerInvariant(), null);
        }

        var section = header[..space].Trim().ToLowerInvariant();
        var subsection = header[space..].Trim();

        if (subsection.StartsWith('"') && subsection.EndsWith('"') && subsection.Length >= 2)
        {
            subsection = subsection[1..^1].Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        return (section, subsection);
    }

    private static string ParseValue(string raw)
    {
        // git config values are unescaped regardless of whether they are quoted: backslash
        // escapes (\\, \", \n, \t, \b) and double-quoted spans are processed inline, so an
        // unquoted Windows path stored as D:\\a\\repo decodes back to D:\a\repo. Comments
        // (# or ;) and trailing whitespace are only significant outside quotes.
        var index = SkipLeadingWhitespace(raw);
        var result = new StringBuilder(raw.Length - index);
        var inQuotes = false;
        var contentEnd = 0;

        while (index < raw.Length)
        {
            var current = raw[index];

            if (current == '\\')
            {
                if (index + 1 >= raw.Length)
                {
                    break;
                }

                result.Append(DecodeEscape(raw[index + 1]));
                contentEnd = result.Length;
                index += 2;
                continue;
            }

            index++;

            if (current == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && IsCommentStart(current))
            {
                break;
            }

            result.Append(current);
            if (inQuotes || !IsWhitespace(current))
            {
                contentEnd = result.Length;
            }
        }

        return result.ToString(0, contentEnd);
    }

    private static int SkipLeadingWhitespace(string raw)
    {
        var index = 0;
        while (index < raw.Length && IsWhitespace(raw[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsWhitespace(char value) => value is ' ' or '\t';

    private static bool IsCommentStart(char value) => value is '#' or ';';

    private static char DecodeEscape(char escaped) => escaped switch
    {
        'n' => '\n',
        't' => '\t',
        'b' => '\b',
        _ => escaped // covers \\ and \" and is lenient for any other escape
    };

    private static string StripComment(string value)
    {
        var inQuotes = false;

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];

            if (current == '"' && (i == 0 || value[i - 1] != '\\'))
            {
                inQuotes = !inQuotes;
            }
            else if (current is '#' or ';' && !inQuotes)
            {
                return value[..i];
            }
        }

        return value;
    }
}
