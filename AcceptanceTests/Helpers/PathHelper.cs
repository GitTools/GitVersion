using System;
using System.IO;
using System.Reflection;

namespace GitHubFlowVersion.AcceptanceTests.Helpers
{
    public static class PathHelper
    {
        public static string GetCurrentDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        } 
    }
}