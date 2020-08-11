using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.AssemblyInfo
{
    [Command("assemblyinfo", "Outputs version to assembly")]
    public class OutputAssemblyInfoOptions : OutputOptions
    {
        [Option("--assemblyinfo-file", "The assembly file")]
        public string AssemblyinfoFile { get; set; }
    }
}