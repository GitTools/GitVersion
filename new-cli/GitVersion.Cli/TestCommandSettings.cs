namespace GitVersion.Commands.Test.Settings;

public record TestCommandSettings : GitVersionSettings
{
    [Option("--input-file", description: "The input version file", aliases: ["-i"])]
    public required string InputFile { get; init; }
}
