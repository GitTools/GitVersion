using GitVersion.Command;

namespace GitVersion.Output.Wix
{
    [Command("wix", typeof(OutputOptions), "Outputs version to wix file")]
    public record OutputWixOptions : OutputOptions
    {
        [Option("--wix-file", "The wix file")]
        public string WixFile { get; init; } = default!;
    }
}