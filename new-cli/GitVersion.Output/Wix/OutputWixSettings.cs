namespace GitVersion.Wix;

public class OutputWixSettings : OutputSettings
{
    [Option("--wix-file", "The wix file")]
    public string WixFile { get; init; } = default;
}
