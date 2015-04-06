namespace GitVersionTask
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Logger = GitVersion.Logger;

    public class UpdateAssemblyInfo : Task
    {
        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public ITaskItem[] CompileFiles { get; set; }

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

        public bool NoFetch { get; set; }

        TaskLogger logger;
        IFileSystem fileSystem;

        public UpdateAssemblyInfo()
        {
            CompileFiles = new ITaskItem[] { };
            logger = new TaskLogger(this);
            fileSystem = new FileSystem();
            Logger.WriteInfo = this.LogInfo;
            Logger.WriteWarning = this.LogWarning;
        }

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

        public void InnerExecute()
        {
            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(CompileFiles, ProjectFile);

            var gitDirectory = GitDirFinder.TreeWalkForDotGitDir(SolutionDirectory);
            if (string.IsNullOrEmpty(gitDirectory))
                return;

            var configuration = ConfigurationProvider.Provide(gitDirectory, fileSystem);

            Tuple<CachedVersion, GitVersionContext> semanticVersion;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out semanticVersion, configuration, NoFetch))
            {
                return;
            }
            CreateTempAssemblyInfo(semanticVersion.Item1, semanticVersion.Item2.Configuration);
        }

        void CreateTempAssemblyInfo(CachedVersion semanticVersion, EffectiveConfiguration configuration)
        {

            if (IntermediateOutputPath == null)
            {
                var tempFileName = string.Format("AssemblyInfo_{0}_{1}.g.cs", Path.GetFileNameWithoutExtension(ProjectFile), Path.GetRandomFileName());
                AssemblyInfoTempFilePath = Path.Combine(TempFileTracker.TempPath, tempFileName);
            }
            else
            {
                AssemblyInfoTempFilePath = Path.Combine(IntermediateOutputPath, "GitVersionTaskAssemblyInfo.g.cs");
            }

            var assemblyInfoBuilder = new AssemblyInfoBuilder
            {
                CachedVersion = semanticVersion
            };
            var assemblyInfo = assemblyInfoBuilder.GetAssemblyInfoText(configuration);
            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo);
        }
    }
}
