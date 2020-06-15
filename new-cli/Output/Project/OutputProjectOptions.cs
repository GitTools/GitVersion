using Core;

namespace Output
{
    [Command("project", "Outputs version to project")]
    public class OutputProjectOptions : OutputOptions
    {
        [Option("--project-file", "The project file")]
        public string ProjectFile { get; set; }
    }
}