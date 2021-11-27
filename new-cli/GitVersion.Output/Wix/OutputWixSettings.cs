using GitVersion.Command;

namespace GitVersion.Output.Wix;

public class OutputWixSettings : OutputSettings
{
    [Option("--wix-file", "The wix file")]
    public string WixFile { get; init; } = default!;
}