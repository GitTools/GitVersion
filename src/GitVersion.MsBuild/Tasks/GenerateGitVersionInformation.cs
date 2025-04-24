using Microsoft.Build.Framework;

namespace GitVersion.MsBuild.Tasks;

public class GenerateGitVersionInformation : GitVersionTaskBase
{
    [Required]
    public string ProjectFile { get; internal init; }

    [Required]
    public string IntermediateOutputPath { get; internal init; }

    [Required]
    public string Language { get; internal init; } = "C#";

    public string? UseProjectNamespaceForGitVersionInformation { get; internal init; }

    public string RootNamespace { get; internal init; }

    [Output]
    public string GitVersionInformationFilePath { get; set; }
}
