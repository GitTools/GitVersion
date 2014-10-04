namespace GitVersionTask
{
    using System;
    using System.Collections.Generic;
    using GitVersion;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Logger = GitVersion.Logger;

    public class WriteVersionInfoToBuildLog : Task
    {
        [Required]
        public string SolutionDirectory { get; set; }

        TaskLogger logger;

        public WriteVersionInfoToBuildLog()
        {
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
            CachedVersion semanticVersion;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out semanticVersion))
            {
                return;
            }

            var authentication = new Authentication();
            WriteIntegrationParameters(semanticVersion, BuildServerList.GetApplicableBuildServers(authentication));
        }

        public void WriteIntegrationParameters(CachedVersion semanticVersion, IEnumerable<IBuildServer> applicableBuildServers)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                logger.LogInfo(string.Format("Executing GenerateSetVersionMessage for '{0}'.", buildServer.GetType().Name));
                logger.LogInfo(buildServer.GenerateSetVersionMessage(semanticVersion.ToString()));
                logger.LogInfo(string.Format("Executing GenerateBuildLogOutput for '{0}'.", buildServer.GetType().Name));
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(semanticVersion.SemanticVersion, buildServer))
                {
                    logger.LogInfo(buildParameter);
                }
            }
        }
    }
}