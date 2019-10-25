// This code originally copied from https://raw.githubusercontent.com/dotnet/sourcelink/master/src/Microsoft.Build.Tasks.Git/GitLoaderContext.cs
#if !NETFRAMEWORK
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;

namespace GitVersion.MSBuildTask.LibGit2Sharp
{
    public sealed class GitLoaderContext : AssemblyLoadContext
    {
        private readonly string[] assemblies;
        public static GitLoaderContext Instance { get; private set; }

        private GitLoaderContext(string[] assemblies) => this.assemblies = assemblies;

        public static void Init(params string[] assemblies) => Instance = new GitLoaderContext(assemblies);

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (assemblies.Contains(assemblyName.Name))
            {
                var path = Path.Combine(Path.GetDirectoryName(typeof(GitLoaderContext).Assembly.Location), assemblyName.Name + ".dll");
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
            var dir = Path.GetDirectoryName(typeof(GitLoaderContext).Assembly.Location);
            return Path.Combine(dir, "runtimes", RuntimeIdMap.GetNativeLibraryDirectoryName(RuntimeEnvironment.GetRuntimeIdentifier()), "native");
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
