// This code originally copied and adapted from https://raw.githubusercontent.com/dotnet/sourcelink/master/src/Microsoft.Build.Tasks.Git/TaskImplementation.cs

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
#endif

using System.Reflection;

namespace GitVersion.MSBuildTask
{
    public class AssemblyProvider : IAssemblyProvider
    {
        public static AssemblyProvider Instance = new AssemblyProvider();

        public Assembly GetAssembly(string tasksAssembly)
        {
            var assy = GitVersionAssemblyLoader.LoadAssembly(tasksAssembly);
            return assy;
        }

    }
}
