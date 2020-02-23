// This code originally copied from https://raw.githubusercontent.com/dotnet/sourcelink/master/src/Microsoft.Build.Tasks.Git/GitLoaderContext.cs
#if !NETFRAMEWORK
using Microsoft.DotNet.PlatformAbstractions;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace GitVersion.MSBuildTask
{
    public sealed class GitVersionAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string[] assemblies;
        public static GitVersionAssemblyLoadContext Instance { get; private set; }

        private GitVersionAssemblyLoadContext(string[] assemblies) => this.assemblies = assemblies;

        public static void Init(params string[] assemblies) => Instance = new GitVersionAssemblyLoadContext(assemblies);

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (assemblies.Contains(assemblyName.Name))
            {
                var path = Path.Combine(Path.GetDirectoryName(typeof(GitVersionAssemblyLoadContext).Assembly.Location), assemblyName.Name + ".dll");
                return LoadFromAssemblyPath(path);
            }

            return Default.LoadFromAssemblyName(assemblyName);
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
            var dir = Path.GetDirectoryName(typeof(GitVersionAssemblyLoadContext).Assembly.Location);
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
