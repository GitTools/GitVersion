using System;
using System.IO;
using System.Reflection;

namespace GitVersionExe.Tests.Helpers
{
    public static class PathHelper
    {
        public static string GetCurrentDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetExecutable()
        {
#if NETFRAMEWORK
            var executable = Path.Combine(GetExeDirectory(), "GitVersion.exe");
#else
        var executable = "dotnet";
#endif
            return executable;
        }

        public static string GetExecutableArgs(string args)
        {
#if !NETFRAMEWORK
        args = $"{Path.Combine(GetExeDirectory(), "GitVersion.dll")} {args}";
#endif
            return args;
        }

        public static string GetTempPath()
        {
            return Path.Combine(GetCurrentDirectory(), "TestRepositories", Guid.NewGuid().ToString());
        }

        private static string GetExeDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace("GitVersionExe.Tests", "GitVersionExe");
        }
    }
}
