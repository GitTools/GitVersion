using System.Text.RegularExpressions;

public static class Scrubbers
{
    public static string GuidScrubber(string value)
    {
        return Regex.Replace(value, @"\b[a-f0-9]{40}\b", "000000000000000000000000000000000000000");
    }
}