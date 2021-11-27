using GitVersion.Command;

namespace GitVersion.Output.Project;

public class OutputProjectSettings : OutputSettings
{
    [Option("--project-file", "The project file")]
    public string ProjectFile { get; init; } = default!;
}