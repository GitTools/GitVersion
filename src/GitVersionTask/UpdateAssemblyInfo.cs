namespace GitVersionTask
{
    using System;
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

            if (!ExecuteCore.TryGetVersion(SolutionDirectory, out var versionVariables, NoFetch, new Authentication()))
            {
                return;
            }

            CreateTempAssemblyInfo(versionVariables);
        }

        void CreateTempAssemblyInfo(VersionVariables versionVariables)
        {
            var fileExtension = GetFileExtension();
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

        string GetFileExtension()
        {
            switch(Language)
            {
                case "C#":
                    return "cs";

                case "F#":
                    return "fs";

                case "VB":
                    return "vb";

                default:
                    throw new Exception($"Unknown language detected: '{Language}'");
            }
        }
    }
}
