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
#if NETDESKTOP
            var is64 = Environment.Is64BitProcess;
#else
            var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            bool is64 = (arch == System.Runtime.InteropServices.Architecture.X64 || arch == System.Runtime.InteropServices.Architecture.Arm64);
            if (arch == System.Runtime.InteropServices.Architecture.X64)
            {
                return "X64";
            }
#endif
            if (is64)
            {
                return "amd64";
            }
            return "x86";
        }
    }
}