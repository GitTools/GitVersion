namespace GitVersion.Commands;

public record OutputWixSettings : OutputSettings
{
    [Option("--wix-file", "The wix file")]
    public required string WixFile { get; init; }
}
