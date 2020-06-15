using Core;

namespace Output
{
    [Command("assemblyinfo", "Outputs version to assembly")]
    public class OutputAssemblyInfoOptions : OutputOptions
    {
        [Option("--assemblyinfo-file", "The assembly file")]
        public string AssemblyinfoFile { get; set; }
    }
}