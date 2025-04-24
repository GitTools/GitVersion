using Microsoft.Build.Framework;

namespace GitVersion.MsBuild.Tasks;

public class UpdateAssemblyInfo : GitVersionTaskBase
{
    [Required]
    public string ProjectFile { get; internal init; }

    [Required]
    public string IntermediateOutputPath { get; internal init; }

    [Required]
    public ITaskItem[] CompileFiles { get; internal init; } = [];

    [Required]
    public string Language { get; internal init; } = "C#";

    [Output]
    public string AssemblyInfoTempFilePath { get; set; }
}
