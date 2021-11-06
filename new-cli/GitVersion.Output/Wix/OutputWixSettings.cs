using GitVersion.Command;

namespace GitVersion.Output.Wix
{
    [Command("wix", typeof(OutputSettings), "Outputs version to wix file")]
    public record OutputWixSettings : OutputSettings
    {
        [Option("--wix-file", "The wix file")]
        public string WixFile { get; init; } = default!;
    }
}