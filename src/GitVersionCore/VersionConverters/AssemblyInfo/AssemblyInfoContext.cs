namespace GitVersion.VersionConverters.AssemblyInfo
{
    public readonly struct AssemblyInfoContext : IConverterContext
    {
        public AssemblyInfoContext(string workingDirectory, bool ensureAssemblyInfo, params string[] assemblyInfoFiles)
        {
            AssemblyInfoFiles = assemblyInfoFiles;
            EnsureAssemblyInfo = ensureAssemblyInfo;
            WorkingDirectory = workingDirectory;
        }

        public string WorkingDirectory { get; }
        public bool EnsureAssemblyInfo { get; }
        public string[] AssemblyInfoFiles { get; }
    }
}
