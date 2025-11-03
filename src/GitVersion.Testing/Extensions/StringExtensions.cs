namespace GitVersion.Testing.Extensions;

public static class StringExtensions
{
    public static IEnumerable<string> SplitIntoLines(this string str, int maxLineLength)
    {
        if (string.IsNullOrEmpty(str)) yield break;

        foreach (var line in SplitByNewlines(str))
        {
            foreach (var wrapped in WrapWithWordBoundaries(line, maxLineLength))
            {
                yield return wrapped;
            }
        }
    }

    private static IEnumerable<string> SplitByNewlines(string str)
        => str.Split(["\r\n", "\n"], StringSplitOptions.None);

    private static IEnumerable<string> WrapWithWordBoundaries(string line, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            yield return string.Empty;
            yield break;
        }

        var index = 0;
        while (index < line.Length)
        {
            var wrapAt = GetWrapIndex(line, index, maxLength);
            yield return line.Substring(index, wrapAt - index).TrimEnd();
            index = wrapAt;
        }
    }

    private static int GetWrapIndex(string line, int start, int maxLength)
    {
        var remaining = line.Length - start;
        if (remaining <= maxLength)
        {
            return line.Length;
        }

        var end = start + maxLength;
        var lastBreak = line.LastIndexOfAny([' ', '-'], end - 1, maxLength);
        return lastBreak > start ? lastBreak + 1 : end;
    }
}
