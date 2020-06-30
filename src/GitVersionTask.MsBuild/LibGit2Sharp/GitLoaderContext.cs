// This code originally copied from https://raw.githubusercontent.com/dotnet/sourcelink/master/src/Microsoft.Build.Tasks.Git/GitLoaderContext.cs
#if !NETFRAMEWORK
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace GitVersion.MSBuildTask.LibGit2Sharp
{
    public sealed class GitLoaderContext : AssemblyLoadContext
    {
        private readonly Assembly entryPointAssembly;
        public static GitLoaderContext Instance { get; private set; }

        private GitLoaderContext(Assembly entryPointAssembly) => this.entryPointAssembly = entryPointAssembly;

        public static void Init(Assembly entryPointAssembly) => Instance = new GitLoaderContext(entryPointAssembly);

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string simpleName = assemblyName.Name;

            if (simpleName == entryPointAssembly.GetName().Name)
            {
                return entryPointAssembly;
            }

            if (simpleName == "Microsoft.Build.Framework")
            {
                // Delegate loading MSBuild types up to an ALC that should already have them
                // once we've gotten this far
                return null;
            }

            var path = Path.Combine(Path.GetDirectoryName(typeof(GitLoaderContext).Assembly.Location), simpleName + ".dll");

            if (File.Exists(path))
            {
                return LoadFromAssemblyPath(path);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var modulePtr = IntPtr.Zero;

            if (unmanagedDllName.StartsWith("git2-", StringComparison.Ordinal) ||
                unmanagedDllName.StartsWith("libgit2-", StringComparison.Ordinal))
            {
                var directory = GetNativeLibraryDirectory();
                var extension = GetNativeLibraryExtension();

                if (!unmanagedDllName.EndsWith(extension, StringComparison.Ordinal))
                {
                    unmanagedDllName += extension;
                }

                var nativeLibraryPath = Path.Combine(directory, unmanagedDllName);
                if (!File.Exists(nativeLibraryPath))
                {
                    nativeLibraryPath = Path.Combine(directory, "lib" + unmanagedDllName);
                }

                modulePtr = LoadUnmanagedDllFromPath(nativeLibraryPath);
            }

            return modulePtr != IntPtr.Zero ? modulePtr : base.LoadUnmanagedDll(unmanagedDllName);
        }

        private static string GetNativeLibraryDirectory()
        {
            var dir = Path.GetDirectoryName(typeof(GitLoaderContext).Assembly.Location);
            return Path.Combine(dir, "runtimes", RuntimeIdMap.GetNativeLibraryDirectoryName(), "native");
        }

        private static string GetNativeLibraryExtension()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ".dll";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return ".dylib";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return ".so";
            }

            throw new PlatformNotSupportedException();
        }
    }
}
#endif
