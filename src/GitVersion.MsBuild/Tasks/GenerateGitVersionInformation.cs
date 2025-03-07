using Microsoft.Build.Framework;

namespace GitVersion.MsBuild.Tasks;

public class GenerateGitVersionInformation : GitVersionTaskBase
{
    [Required]
    public string ProjectFile { get; set; }

    [Required]
    public string IntermediateOutputPath { get; set; }

    [Required]
    public string Language { get; set; } = "C#";

    public string? UseProjectNamespaceForGitVersionInformation { get; set; }

    public string RootNamespace { get; set; }

    [Output]
    public string GitVersionInformationFilePath { get; set; }
}
