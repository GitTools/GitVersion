using System;
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
        public ITaskItem[] CompileFiles { get; set; } = Array.Empty<ITaskItem>();

        [Required]
        public string Language { get; set; } = "C#";

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

        protected override bool OnExecute() => TaskProxy.UpdateAssemblyInfo(this);
    }
}
