using System;
using System.Reflection;
using GitVersion.MSBuildTask.LibGit2Sharp;
using GitVersion.MSBuildTask.Tasks;

namespace GitVersion.MSBuildTask
{
    public static class TaskProxy
    {
        public static Func<GetVersion, bool> GetVersion;
        public static Func<GenerateGitVersionInformation, bool> GenerateGitVersionInformation;
        public static Func<UpdateAssemblyInfo, bool> UpdateAssemblyInfo;
        public static Func<WriteVersionInfoToBuildLog, bool> WriteVersionInfoToBuildLog;

        static TaskProxy()
        {
#if !NETFRAMEWORK
            GitLoaderContext.Init("GitVersionCore", "LibGit2Sharp");
#endif
            LibGit2SharpLoader.LoadAssembly("GitVersionTask");

            var type = LibGit2SharpLoader.Instance.Assembly.GetType("GitVersion.MSBuildTask.GitVersionTasks", throwOnError: true).GetTypeInfo();

            GetVersion                    = GetMethod<GetVersion>(type, nameof(GetVersion));
            GenerateGitVersionInformation = GetMethod<GenerateGitVersionInformation>(type, nameof(GenerateGitVersionInformation));
            UpdateAssemblyInfo            = GetMethod<UpdateAssemblyInfo>(type, nameof(UpdateAssemblyInfo));
            WriteVersionInfoToBuildLog    = GetMethod<WriteVersionInfoToBuildLog>(type, nameof(WriteVersionInfoToBuildLog));
        }

        private static Func<T, bool> GetMethod<T>(TypeInfo type, string name) => (Func<T, bool>)type.GetDeclaredMethod(name).CreateDelegate(typeof(Func<T, bool>));
    }
}
