using GitVersion.Command;

namespace GitVersion.Output.AssemblyInfo
{
    [Command("assemblyinfo", typeof(OutputCommand), "Outputs version to assembly")]
    public record OutputAssemblyInfoCommand : OutputCommand
    {
        [Option("--assemblyinfo-file", "The assembly file")]
        public string AssemblyinfoFile { get; init; } = default!;
    }
}