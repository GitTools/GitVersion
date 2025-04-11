using GitVersion.Extensions;

namespace GitVersion;

internal static class QuotedStringHelpers
{
    /// <summary>
    /// Splits input string based on split-character, ignoring split-character in
    /// quoted part of the string.
    /// </summary>
    /// <param name="input">String we want to split.</param>
    /// <param name="splitChar">Character used for splitting.</param>
    /// <returns>Array of split string parts</returns>
    /// <remarks>
    /// If there is opening quotes character without closing quotes,
    /// closing quotes are implicitly assumed at the end of the input string.
    /// </remarks>
    /// <example>
    /// "one two three" -> {"one", "two",  "three"}
    /// "one \"two three\"" -> {"one", "\"two three\""}
    /// "one \"two three" -> {"one", "\"two three"} // implicit closing quote.
    /// </example>
    public static string[] SplitUnquoted(string? input, char splitChar)
    {
        if (input == null)
            return [];

        var split = new List<string>();
        var isPreviousCharBackslash = false;
        var isInsideQuotes = false;

        var startIndex = 0;
        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];
            switch (current)
            {
                case '"':
                    if (!isPreviousCharBackslash)
                        isInsideQuotes = !isInsideQuotes;
                    break;
                default:
                    if (current == splitChar && !isInsideQuotes)
                    {
                        split.Add(input.Substring(startIndex, i - startIndex));
                        startIndex = i + 1;
                    }
                    break;
            }
            isPreviousCharBackslash = current == '\\';
        }

        split.Add(input[startIndex..]);

        return split.Where(argument => !argument.IsNullOrEmpty()).ToArray();
    }

    /// <summary>
    /// Removes enclosing quotes around input string and unescapes quote characters
    /// inside of string.
    /// </summary>
    /// <param name="input">Input string to unescape.</param>
    /// <returns>Unescaped string.</returns>
    /// <example>
    /// "\"one \\\"two\\\"\"" -> "one \"two\""
    /// </example>
    public static string UnquoteText(string input)
    {
        var sb = new StringBuilder(input);

        if (sb[0] == '"')
            sb.Remove(0, 1);

        if (sb[^1] == '"' && sb[^2] != '\\')
            sb.Remove(sb.Length - 1, 1);

        sb.Replace("\\\"", "\""); // unescape quotes.

        return sb.ToString();
    }
}
