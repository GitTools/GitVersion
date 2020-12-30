using GitVersion.Command;

namespace GitVersion.Output.Project
{
    [Command("project", typeof(OutputCommand), "Outputs version to project")]
    public record OutputProjectCommand : OutputCommand
    {
        [Option("--project-file", "The project file")]
        public string ProjectFile { get; init; } = default!;
    }
}