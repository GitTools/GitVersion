namespace GitVersionTask
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersion.Configuration;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Logger = GitVersion.Logger;

    public class UpdateAssemblyInfo : Task
    {
        public string AssemblyVersioningScheme { get; set; }

        public string DevelopBranchTag { get; set; }

        public string ReleaseBranchTag { get; set; }

        public string TagPrefix { get; set; }

        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string ProjectFile { get; set; }
        [Required]
        public ITaskItem[] CompileFiles { get; set; }

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

        TaskLogger logger;

        public UpdateAssemblyInfo()
        {
            CompileFiles = new ITaskItem[] { };
            logger = new TaskLogger(this);
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

            var configuration = ConfigurationProvider.Provide(gitDirectory);
            if (!string.IsNullOrEmpty(AssemblyVersioningScheme))
            {
                AssemblyVersioningScheme versioningScheme;
                if (Enum.TryParse(AssemblyVersioningScheme, true, out versioningScheme))
                {
                    configuration.AssemblyVersioningScheme = versioningScheme;
                }
                else
                {
                    throw new WarningException(string.Format("Unexpected assembly versioning scheme '{0}'.", AssemblyVersioningScheme));
                }
            }

            // TODO This should be covered by tests
            // Null is intentional. Empty string means the user has set the value to an empty string and wants to clear the tag
            if (DevelopBranchTag != null)
            {
                configuration.DevelopBranchTag = DevelopBranchTag;
            }

            if (ReleaseBranchTag != null)
            {
                configuration.ReleaseBranchTag = ReleaseBranchTag;
            }

            if (TagPrefix != null)
            {
                configuration.TagPrefix = TagPrefix;
            }

            CachedVersion semanticVersion;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out semanticVersion, configuration))
            {
                return;
            }
            CreateTempAssemblyInfo(semanticVersion, configuration);
        }

        void CreateTempAssemblyInfo(CachedVersion semanticVersion, Config configuration)
        {
            var assemblyInfoBuilder = new AssemblyInfoBuilder
                                      {
                                          CachedVersion = semanticVersion
                                      };
            var assemblyInfo = assemblyInfoBuilder.GetAssemblyInfoText(configuration);

            var tempFileName = string.Format("AssemblyInfo_{0}_{1}.g.cs", Path.GetFileNameWithoutExtension(ProjectFile), Path.GetRandomFileName());
            AssemblyInfoTempFilePath = Path.Combine(TempFileTracker.TempPath, tempFileName);
            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo);
        }
    }
}