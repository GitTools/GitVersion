namespace GitVersion.Output.AssemblyInfo;

internal readonly record struct AssemblyInfoContext(string WorkingDirectory, bool EnsureAssemblyInfo, params string[] AssemblyInfoFiles)
    : IConverterContext;
