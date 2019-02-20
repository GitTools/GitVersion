namespace GitVersion
{
    using System;
    using System.IO;

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
            if (Environment.Is64BitProcess)
            {
                return "amd64";
            }
            return "x86";
        }
    }
}