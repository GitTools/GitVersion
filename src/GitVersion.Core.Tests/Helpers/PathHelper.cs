using System;
using System.IO;
using System.Reflection;

namespace GitVersion.Core.Tests.Helpers
{
    public static class PathHelper
    {
        public static string GetCurrentDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetExecutable()
        {
            return RuntimeHelper.IsCoreClr() ? "dotnet" : Path.Combine(GetExeDirectory(), "gitversion.exe");
        }

        public static string GetExecutableArgs(string args)
        {
            if (RuntimeHelper.IsCoreClr())
            {
                args = $"{Path.Combine(GetExeDirectory(), "gitversion.dll")} {args}";
            }
            return args;
        }

        public static string GetTempPath()
        {
            return Path.Combine(GetCurrentDirectory(), "TestRepositories", Guid.NewGuid().ToString());
        }

        private static string GetExeDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)?.Replace("GitVersion.App.Tests", "GitVersion.App");
        }
    }
}
