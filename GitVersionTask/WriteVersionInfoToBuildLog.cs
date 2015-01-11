namespace GitVersionTask
{
    using System;
    using System.Collections.Generic;
    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Logger = GitVersion.Logger;

    public class WriteVersionInfoToBuildLog : Task
    {
        [Required]
        public string SolutionDirectory { get; set; }

        TaskLogger logger;
        IFileSystem fileSystem;

        public WriteVersionInfoToBuildLog()
        {
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
            CachedVersion semanticVersion;
            var gitDirectory = GitDirFinder.TreeWalkForGitDir(SolutionDirectory);
            var configuration = ConfigurationProvider.Provide(gitDirectory, fileSystem);
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out semanticVersion, configuration))
            {
                return;
            }

            var authentication = new Authentication();
            WriteIntegrationParameters(semanticVersion, BuildServerList.GetApplicableBuildServers(authentication));
        }

        public void WriteIntegrationParameters(CachedVersion cachedVersion, IEnumerable<IBuildServer> applicableBuildServers)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                logger.LogInfo(string.Format("Executing GenerateSetVersionMessage for '{0}'.", buildServer.GetType().Name));
                logger.LogInfo(buildServer.GenerateSetVersionMessage(cachedVersion.SemanticVersion.ToString()));
                logger.LogInfo(string.Format("Executing GenerateBuildLogOutput for '{0}'.", buildServer.GetType().Name));
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(cachedVersion.SemanticVersion, buildServer))
                {
                    logger.LogInfo(buildParameter);
                }
            }
        }
    }
}