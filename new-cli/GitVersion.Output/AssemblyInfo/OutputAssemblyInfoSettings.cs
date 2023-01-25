namespace GitVersion.Commands;

public record OutputAssemblyInfoSettings : OutputSettings
{
    [Option("--assemblyinfo-file", "The assembly file")]
    public required string AssemblyinfoFile { get; init; }
}
