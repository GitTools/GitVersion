using GitVersion.Command;

namespace GitVersion.Output.AssemblyInfo;

[Command("assemblyinfo", typeof(OutputSettings), "Outputs version to assembly")]
public record OutputAssemblyInfoSettings : OutputSettings
{
    [Option("--assemblyinfo-file", "The assembly file")]
    public string AssemblyinfoFile { get; init; } = default!;
}