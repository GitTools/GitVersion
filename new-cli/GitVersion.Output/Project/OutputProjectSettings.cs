using GitVersion.Command;

namespace GitVersion.Output.Project;

[Command("project", typeof(OutputSettings), "Outputs version to project")]
public record OutputProjectSettings : OutputSettings
{
    [Option("--project-file", "The project file")]
    public string ProjectFile { get; init; } = default!;
}