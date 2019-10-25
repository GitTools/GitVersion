using System;
using System.IO;

namespace GitVersion.MSBuildTask.Tests
{
    public static class AssemblyLocation
    {
        public static string CurrentDirectory()
        {
            var assembly = typeof(AssemblyLocation).Assembly;
            var uri = new UriBuilder(assembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);

            return Path.GetDirectoryName(path);
        }
    }
}