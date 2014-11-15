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

            CachedVersion semanticVersion;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out semanticVersion))
            {
                return;
            }

            CreateTempAssemblyInfo(semanticVersion);
        }

        AssemblyVersioningScheme GetAssemblyVersioningScheme()
        {
            if (string.IsNullOrWhiteSpace(AssemblyVersioningScheme))
            {
                var gitDirectory = GitDirFinder.TreeWalkForGitDir(SolutionDirectory);

                var configFilePath = Path.Combine(Directory.GetParent(gitDirectory).FullName, "GitVersionConfig.yaml");
                if (File.Exists(configFilePath))
                {
                    using (var reader = File.OpenText(configFilePath))
                    {
                        return ConfigReader.Read(reader).AssemblyVersioningScheme;
                    }
                }
                return GitVersion.AssemblyVersioningScheme.MajorMinorPatch;
            }

            AssemblyVersioningScheme versioningScheme;

            if (Enum.TryParse(AssemblyVersioningScheme, true, out versioningScheme))
            {
                return versioningScheme;
            }

            throw new WarningException(string.Format("Unexpected assembly versioning scheme '{0}'.", AssemblyVersioningScheme));
        }

        void CreateTempAssemblyInfo(CachedVersion semanticVersion)
        {
            var versioningScheme = GetAssemblyVersioningScheme();
            var assemblyInfoBuilder = new AssemblyInfoBuilder
                                      {
                                          CachedVersion = semanticVersion,
                                          AssemblyVersioningScheme = versioningScheme,
                                      };
            var assemblyInfo = assemblyInfoBuilder.GetAssemblyInfoText();

            var tempFileName = string.Format("AssemblyInfo_{0}_{1}.g.cs", Path.GetFileNameWithoutExtension(ProjectFile), Path.GetRandomFileName());
            AssemblyInfoTempFilePath = Path.Combine(TempFileTracker.TempPath, tempFileName);
            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo);
        }
    }
}