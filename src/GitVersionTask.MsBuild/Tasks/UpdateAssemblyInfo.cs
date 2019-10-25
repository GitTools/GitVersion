using Microsoft.Build.Framework;

namespace GitVersion.MSBuildTask.Tasks
{
    public class UpdateAssemblyInfo : GitVersionTaskBase
    {
        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public ITaskItem[] CompileFiles { get; set; }

        [Required]
        public string Language { get; set; }

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

        public override bool Execute() => TaskProxy.UpdateAssemblyInfo(this);
    }
}
