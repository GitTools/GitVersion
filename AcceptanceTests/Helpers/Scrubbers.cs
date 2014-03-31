using System.Text.RegularExpressions;

namespace AcceptanceTests.Helpers
{
    public static class Scrubbers
    {
        public static string GuidScrubber(string value)
        {
            return Regex.Replace(value, @"\b[a-f0-9]{40}\b", "000000000000000000000000000000000000000");
        }
    }
}