using GitVersion.Command;

namespace GitVersion.Output.Wix
{
    [Command("wix", typeof(OutputCommand), "Outputs version to wix file")]
    public record OutputWixCommand : OutputCommand
    {
        [Option("--wix-file", "The wix file")]
        public string WixFile { get; init; } = default!;
    }
}