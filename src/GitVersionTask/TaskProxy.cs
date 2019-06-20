namespace GitVersionTask
{
    using System;
    using System.Reflection;

    public static class TaskProxy
    {
        public static Func<GetVersion, bool> GetVersion;
        public static Func<GenerateGitVersionInformation, bool> GenerateGitVersionInformation;
        public static Func<UpdateAssemblyInfo, bool> UpdateAssemblyInfo;
        public static Func<WriteVersionInfoToBuildLog, bool> WriteVersionInfoToBuildLog;

        static TaskProxy()
        {
            var type = typeof(GitVersionTasks).GetTypeInfo();

            GetVersion                    = GetMethod<GetVersion>(type, nameof(GetVersion));
            GenerateGitVersionInformation = GetMethod<GenerateGitVersionInformation>(type, nameof(GenerateGitVersionInformation));
            UpdateAssemblyInfo            = GetMethod<UpdateAssemblyInfo>(type, nameof(UpdateAssemblyInfo));
            WriteVersionInfoToBuildLog    = GetMethod<WriteVersionInfoToBuildLog>(type, nameof(WriteVersionInfoToBuildLog));
        }

        private static Func<T, bool> GetMethod<T>(TypeInfo type, string name) => (Func<T, bool>)type.GetDeclaredMethod(name).CreateDelegate(typeof(Func<T, bool>));
    }
}
