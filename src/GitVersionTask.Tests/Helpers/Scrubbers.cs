using System.Text.RegularExpressions;

namespace GitVersion.MSBuildTask.Tests.Helpers
{
    public static class Scrubbers
    {
        public static string GuidScrubber(string value)
        {
            return Regex.Replace(value, @"\b[a-f0-9]{40}\b", "000000000000000000000000000000000000000");
        }
        public static string GuidAndDateScrubber(string value)
        {
            return Regex.Replace(GuidScrubber(value), @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(.\d*?)?Z", "<date replaced>");
        }
    }
}