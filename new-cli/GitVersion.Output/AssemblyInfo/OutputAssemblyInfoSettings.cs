namespace GitVersion.AssemblyInfo;

public class OutputAssemblyInfoSettings : OutputSettings
{
    [Option("--assemblyinfo-file", "The assembly file")]
    public string AssemblyinfoFile { get; init; } = default;
}
