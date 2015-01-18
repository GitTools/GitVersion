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

            var gitDirectory = GitDirFinder.TreeWalkForGitDir(SolutionDirectory);
            if (string.IsNullOrEmpty(gitDirectory))
                return;

            var configuration = ConfigurationProvider.Provide(gitDirectory, fileSystem);

            Tuple<CachedVersion, GitVersionContext> semanticVersion;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out semanticVersion, configuration))
            {
                return;
            }
            CreateTempAssemblyInfo(semanticVersion.Item1, semanticVersion.Item2.Configuration);
        }

        void CreateTempAssemblyInfo(CachedVersion semanticVersion, EffectiveConfiguration configuration)
        {
            var assemblyInfoBuilder = new AssemblyInfoBuilder
                                      {
                                          CachedVersion = semanticVersion
                                      };
            var assemblyInfo = assemblyInfoBuilder.GetAssemblyInfoText(configuration);

            string tempFileName;
            string tempDir;
            if (IntermediateOutputPath == null)
            {
                tempDir = TempFileTracker.TempPath;
                tempFileName = string.Format("AssemblyInfo_{0}_{1}.g.cs", Path.GetFileNameWithoutExtension(ProjectFile), Path.GetRandomFileName());
            }
            else
            {
                tempDir = Path.Combine(IntermediateOutputPath, "obj", assemblyInfo);
                Directory.CreateDirectory(tempDir);
                tempFileName = string.Format("GitVersionTaskAssemblyInfo.g.cs");
            }

            AssemblyInfoTempFilePath = Path.Combine(tempDir, tempFileName);
            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo);
        }
    }
}
