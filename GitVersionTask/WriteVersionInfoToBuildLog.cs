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

        public void InnerExecute()
        {
            VersionAndBranchAndDate versionAndBranch;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out versionAndBranch))
            {
                return;
            }

            WriteIntegrationParameters(versionAndBranch, BuildServerList.GetApplicableBuildServers());
        }

        public void WriteIntegrationParameters(VersionAndBranch versionAndBranch, IEnumerable<IBuildServer> applicableBuildServers)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                logger.LogInfo(string.Format("Executing GenerateSetVersionMessage for '{0}'.", buildServer.GetType().Name));
                logger.LogInfo(buildServer.GenerateSetVersionMessage(versionAndBranch.GenerateSemVer()));
                logger.LogInfo(string.Format("Executing GenerateBuildLogOutput for '{0}'.", buildServer.GetType().Name));
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(versionAndBranch, buildServer))
                {
                    logger.LogInfo(buildParameter);
                }
            }
        }
  
    }
}