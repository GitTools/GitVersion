// This code originally copied and adapted from https://raw.githubusercontent.com/dotnet/sourcelink/master/src/Microsoft.Build.Tasks.Git/TaskImplementation.cs

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
#endif

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GitVersion.MSBuildTask
{

    public static class GitVersionAssemblyLoader
    {
        private static readonly string TaskDirectory = Path.GetDirectoryName(typeof(GitVersionAssemblyLoader).Assembly.Location);

       // public static GitVersionAssemblyLoader Instance { get; private set; }

#if !NETFRAMEWORK

        private static Lazy<GitVersionAssemblyLoadContext> LoadContext = new Lazy<GitVersionAssemblyLoadContext>(() =>
        {
            GitVersionAssemblyLoadContext.Init("GitVersionCore", "LibGit2Sharp", "Microsoft.Extensions.DependencyInjection");
            return GitVersionAssemblyLoadContext.Instance;
        });

#endif

        public static Assembly LoadAssembly(string tasksAssembly)
        {
#if NETFRAMEWORK
            // nullVersion = new Version(0, 0, 0, 0);
            loaderLog = new List<string>();

            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

            var assemblyName = typeof(GitVersionAssemblyLoader).Assembly.GetName();
            assemblyName.Name = tasksAssembly;
            var assembly = Assembly.Load(assemblyName);

            // I considered this, but other locations in the code cause other dependencies to be loaded
            // such as Microsoft.Extensions.DependencyInjection, so leave the handler active.
           // AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
#else
            var operationsPath = Path.Combine(TaskDirectory, tasksAssembly + ".dll");
            var assembly = LoadContext.Value.LoadFromAssemblyPath(operationsPath);
#endif

            return assembly;
        }

#if NETFRAMEWORK

        //   private static Version nullVersion;

        private static List<string> loaderLog;

        private static List<string> Whitelist = new List<string>() { "System.", "Microsoft." };

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
            if (!Whitelist.Any(a => referenceName.Name.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
            {
                Log(args, "not whitelisted.");
                return null;
            }

            //if (referenceName.Version != nullVersion)
            //{
            //    Log(args, "not null version");
            //    return null;
            //}

            var referencePath = Path.Combine(TaskDirectory, referenceName.Name + ".dll");
            if (!File.Exists(referencePath))
            {
                if (referenceName.Name.StartsWith("System.Runtime.Loader"))
                {
                    throw new Exception("Asked to load: " + referenceName.ToString() + " but that doesn't exist");
                }
                Log(args, $"file '{referencePath}' not found");
                return null;
            }

            Log(args, $"loading from '{referencePath}'");
            var assyName = AssemblyName.GetAssemblyName(referencePath);            
         
            return Assembly.Load(assyName);
        }
#endif
    }
}
