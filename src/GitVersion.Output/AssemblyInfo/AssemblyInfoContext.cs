namespace GitVersion.Output.AssemblyInfo;

internal readonly struct AssemblyInfoContext(string workingDirectory, bool ensureAssemblyInfo, params string[] assemblyInfoFiles)
    : IConverterContext
{
    public string WorkingDirectory { get; } = workingDirectory;
    public bool EnsureAssemblyInfo { get; } = ensureAssemblyInfo;
    public string[] AssemblyInfoFiles { get; } = assemblyInfoFiles;
}
