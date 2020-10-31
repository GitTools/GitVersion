using GitVersion.Command;

namespace GitVersion.Output.AssemblyInfo
{
    [Command("assemblyinfo", "Outputs version to assembly")]
    public record OutputAssemblyInfoOptions : OutputOptions
    {
        [Option("--assemblyinfo-file", "The assembly file")]
        public string AssemblyinfoFile { get; init; } = default!;
    }
}