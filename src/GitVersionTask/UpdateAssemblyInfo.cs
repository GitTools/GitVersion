namespace GitVersionTask
{
    using System;
    using System.IO;
    using System.Text;

    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;
    using GitTools;

    public class UpdateAssemblyInfo : GitVersionTaskBase
    {
        TaskLogger logger;

        public UpdateAssemblyInfo()
        {
            logger = new TaskLogger(this);
            Logger.SetLoggers(this.LogDebug, this.LogInfo, this.LogWarning, s => this.LogError(s));
        }

        [Required]
        public string SolutionDirectory { get; set; }

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

        public bool NoFetch { get; set; }

        public override bool Execute()
        {
            try
            {
                InnerExecute();
                return true;
            }
            catch (WarningException errorException)
            {
                logger.LogWarning(errorException.Message);
                return true;
            }
            catch (Exception exception)
            {
                logger.LogError("Error occurred: " + exception);
                return false;
            }
            finally
            {
                Logger.Reset();
            }
        }

        void InnerExecute()
        {
            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(CompileFiles, ProjectFile);

            VersionVariables versionVariables;
            if (!ExecuteCore.TryGetVersion(SolutionDirectory, out versionVariables, NoFetch, new Authentication()))
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