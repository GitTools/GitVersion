using GitVersion.Command;

namespace GitVersion.Output.AssemblyInfo;

public class OutputAssemblyInfoSettings : OutputSettings
{
    [Option("--assemblyinfo-file", "The assembly file")]
    public string AssemblyinfoFile { get; init; } = default!;
}