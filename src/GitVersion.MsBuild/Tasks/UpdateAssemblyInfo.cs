using Microsoft.Build.Framework;

namespace GitVersion.MsBuild.Tasks;

public class UpdateAssemblyInfo : GitVersionTaskBase
{
    [Required]
    public string ProjectFile { get; internal init; } = null!;

    [Required]
    public string IntermediateOutputPath { get; internal init; } = null!;

    [Required]
    public ITaskItem[] CompileFiles { get; internal init; } = [];

    [Required]
    public string Language { get; internal init; } = "C#";

    [Output]
    public string AssemblyInfoTempFilePath { get; set; } = null!;
}
