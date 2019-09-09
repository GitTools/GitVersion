using System;
using System.IO;

namespace GitVersion
{
    class SearchPath
    {
        static bool isPathSet;

        public static void SetSearchPath(string addinDirectoryPath)
        {
            if (isPathSet)
            {
                return;
            }
            isPathSet = true;
            var nativeBinaries = Path.Combine(addinDirectoryPath, "NativeBinaries", GetProcessorArchitecture());
            var existingPath = Environment.GetEnvironmentVariable("PATH");
            var newPath = string.Concat(nativeBinaries, Path.PathSeparator, existingPath);
            Environment.SetEnvironmentVariable("PATH", newPath);
        }

        static string GetProcessorArchitecture()
        {
            return Environment.Is64BitProcess ? "amd64" : "x86";
        }
    }
}