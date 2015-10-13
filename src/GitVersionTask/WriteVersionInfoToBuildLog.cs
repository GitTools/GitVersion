namespace GitVersionTask
{
    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using Logger = GitVersion.Logger;

    public class WriteVersionInfoToBuildLog : Task
    {
        [Required]
        public string SolutionDirectory { get; set; }

        public bool NoFetch { get; set; }

        TaskLogger logger;
        IFileSystem fileSystem;

        public WriteVersionInfoToBuildLog()
        {
            logger = new TaskLogger(this);
            fileSystem = new FileSystem();
            Logger.SetLoggers(
                this.LogInfo,
                this.LogWarning,
                s => this.LogError(s));
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
            VersionVariables result;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out result, NoFetch, new Authentication(), fileSystem))
            {
                return;
            }
            
            WriteIntegrationParameters(BuildServerList.GetApplicableBuildServers(), result);
        }

        public void WriteIntegrationParameters(IEnumerable<IBuildServer> applicableBuildServers, VersionVariables variables)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                logger.LogInfo(string.Format("Executing GenerateSetVersionMessage for '{0}'.", buildServer.GetType().Name));
                logger.LogInfo(buildServer.GenerateSetVersionMessage(variables));
                logger.LogInfo(string.Format("Executing GenerateBuildLogOutput for '{0}'.", buildServer.GetType().Name));
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, variables))
                {
                    logger.LogInfo(buildParameter);
                }
            }
        }
    }
}