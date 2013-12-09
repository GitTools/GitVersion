namespace GitFlowVersionTask
{
    using System;
    using System.IO;
    using GitFlowVersion;
    using GitFlowVersion.VersionBuilders;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Logger = GitFlowVersion.Logger;

    public class UpdateAssemblyInfo : Task
    {

        public bool SignAssembly { get; set; }

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
            CompileFiles = new ITaskItem[] {};
            logger = new TaskLogger(this);
            Logger.WriteInfo = this.LogInfo;
        }

        public override bool Execute()
        {
            try
            {
                return InnerExecute();
            }
            catch (ErrorException errorException)
            {
                logger.LogError(errorException.Message);
                return false;
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

        public bool InnerExecute()
        {
            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(CompileFiles, ProjectFile);

            var gitDirectory = GitDirFinder.TreeWalkForGitDir(SolutionDirectory);

            if (string.IsNullOrEmpty(gitDirectory))
            {
                var message =
                    "No .git directory found in provided solution path. This means the assembly may not be versioned correctly. " +
                    "To fix this warning either clone the repository using git or remove the `GitFlowVersion.Fody` nuget package. " +
                    "To temporarily work around this issue add a AssemblyInfo.cs with an appropriate `AssemblyVersionAttribute`." +
                    "If it is detected that this build is occurring on a CI server an error may be thrown.";
                logger.LogWarning(message);
                return true;
            }

            var versionAndBranch = VersionCache.GetVersion(gitDirectory);

            WriteIntegrationParameters(versionAndBranch, gitDirectory);
            CreateTempAssemblyInfo(versionAndBranch);

            return true;
        }

        public virtual void WriteIntegrationParameters(VersionAndBranch versionAndBranch, string gitDirectory)
        {
            foreach (var buildServer in BuildServerList.BuildServers)
            {
                if (!buildServer.CanApplyToCurrentContext())
                {
                    continue;
                }
                logger.LogInfo(string.Format("Executing inside a {0} build agent.", buildServer.GetType().Name));

                logger.LogWarning(buildServer.GenerateSetVersionMessage(versionAndBranch.GenerateSemVer()));

                buildServer.PerformPreProcessingSteps(gitDirectory);
                foreach (var buildParameter in BuildOutputGenerator.GenerateBuildLogOutput(versionAndBranch, buildServer))
                {
                    logger.LogWarning(buildParameter);
                }
            }
        }
  

        void CreateTempAssemblyInfo(VersionAndBranch versionAndBranch)
        {
            var assemblyInfoBuilder = new AssemblyInfoBuilder
                                      {
                                          VersionAndBranch = versionAndBranch,
                                          SignAssembly = SignAssembly
                                      };
            var assemblyInfo = assemblyInfoBuilder.GetAssemblyInfoText();

            var tempFileName = string.Format("AssemblyInfo_{0}_{1}.cs", Path.GetFileNameWithoutExtension(ProjectFile), Path.GetRandomFileName());
            AssemblyInfoTempFilePath = Path.Combine(TempFileTracker.TempPath, tempFileName);
            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo);
        }


    }
}