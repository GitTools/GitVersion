namespace GitVersionTask
{
    using System.IO;

    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;

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

        protected override void InnerExecute()
        {
            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(CompileFiles, ProjectFile);

            if (GetVersionVariables(out var versionVariables)) return;

            CreateTempAssemblyInfo(versionVariables);
        }

        private void CreateTempAssemblyInfo(VersionVariables versionVariables)
        {
            var fileExtension = TaskUtils.GetFileExtension(Language);
            var assemblyInfoFileName = $"GitVersionTaskAssemblyInfo.g.{fileExtension}";

            if (IntermediateOutputPath == null)
            {
                assemblyInfoFileName = $"AssemblyInfo_{Path.GetFileNameWithoutExtension(ProjectFile)}_{Path.GetRandomFileName()}.g.{fileExtension}";
            }

            var workingDirectory = IntermediateOutputPath ?? TempFileTracker.TempPath;

            AssemblyInfoTempFilePath = Path.Combine(workingDirectory, assemblyInfoFileName);

            using (var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(assemblyInfoFileName, workingDirectory, versionVariables, new FileSystem(), true))
            {
                assemblyInfoFileUpdater.Update();
                assemblyInfoFileUpdater.CommitChanges();
            }
        }
    }
}
