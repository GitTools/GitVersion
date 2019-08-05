// This code originally copied and adapted from https://raw.githubusercontent.com/dotnet/sourcelink/master/src/Microsoft.Build.Tasks.Git/TaskImplementation.cs

namespace GitVersionTask.MsBuild.LibGit2Sharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    public class LibGit2SharpLoader
    {
        private static string taskDirectory;

        public static LibGit2SharpLoader Instance { get; private set; }
        public Assembly Assembly { get; }

        public static void LoadAssembly(string tasksAssembly) => Instance = new LibGit2SharpLoader(tasksAssembly);

        private LibGit2SharpLoader(string tasksAssembly)
        {
            taskDirectory = Path.GetDirectoryName(typeof(LibGit2SharpLoader).Assembly.Location);
#if NET472
            nullVersion = new Version(0, 0, 0, 0);
            loaderLog = new List<string>();

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            var assemblyName = typeof(LibGit2SharpLoader).Assembly.GetName();
            assemblyName.Name = tasksAssembly;
            Assembly = Assembly.Load(assemblyName);
#else
            var operationsPath = Path.Combine(taskDirectory, tasksAssembly + ".dll");
            Assembly = GitLoaderContext.Instance.LoadFromAssemblyPath(operationsPath);
#endif
        }

#if NET472

        private static Version nullVersion;

        private static List<string> loaderLog;

        private static void Log(ResolveEventArgs args, string outcome)
        {
            lock (loaderLog)
            {
                loaderLog.Add($"Loading '{args.Name}' referenced by '{args.RequestingAssembly}': {outcome}.");
            }
        }

        public static string[] GetLog()
        {
            lock (loaderLog)
            {
                return loaderLog.ToArray();
            }
        }

        private static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Limit resolution scope to minimum to affect the rest of msbuild as little as possible.
            // Only resolve System.* assemblies from the task directory that are referenced with 0.0.0.0 version (from netstandard.dll).

            var referenceName = new AssemblyName(args.Name);
            if (!referenceName.Name.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
            {
                Log(args, "not System");
                return null;
            }

            if (referenceName.Version != nullVersion)
            {
                Log(args, "not null version");
                return null;
            }

            var referencePath = Path.Combine(taskDirectory, referenceName.Name + ".dll");
            if (!File.Exists(referencePath))
            {
                Log(args, $"file '{referencePath}' not found");
                return null;
            }

            Log(args, $"loading from '{referencePath}'");
            return Assembly.Load(AssemblyName.GetAssemblyName(referencePath));
        }
#endif
    }
}
