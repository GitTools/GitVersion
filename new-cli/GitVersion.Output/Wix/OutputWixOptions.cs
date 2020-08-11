using GitVersion.Command;
using GitVersion.Infrastructure;

namespace GitVersion.Output.Wix
{
    [Command("wix", "Outputs version to wix file")]
    public class OutputWixOptions : OutputOptions
    {
        [Option("--wix-file", "The wix file")]
        public string WixFile { get; set; }
    }
}