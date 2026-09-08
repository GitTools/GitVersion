using AngleSharp.Dom;

namespace Docs.Utilities;

public static class DocsLinks
{
    private static readonly char[] PathSuffixes = ['#', '?'];

    public static string PathOnly(string value) => value.Split(PathSuffixes)[0];

    public static bool IsRootRelative(string value) => value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal);

    public static string? RefreshTarget(IElement element)
    {
        if (!string.Equals(element.GetAttribute("http-equiv"), "refresh", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var content = element.GetAttribute("content") ?? "";
        var separator = content.IndexOf("url=", StringComparison.OrdinalIgnoreCase);
        return separator < 0 ? null : content[(separator + 4)..].Trim().Trim('\'', '"');
    }
}
