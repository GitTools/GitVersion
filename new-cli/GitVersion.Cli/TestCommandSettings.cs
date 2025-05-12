namespace GitVersion.Commands.Test.Settings;

public record TestCommandSettings : GitVersionSettings
{
    [Option("--input-file", "The input version file")]
    public required string InputFile { get; init; }
}
