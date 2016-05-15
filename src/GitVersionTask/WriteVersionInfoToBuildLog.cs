namespace GitVersionTask
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    using GitVersion;

    using Microsoft.Build.Framework;

    public class WriteVersionInfoToBuildLog : GitVersionTaskBase
    {
        readonly TaskLogger logger;

        public WriteVersionInfoToBuildLog()
        {
            logger = new TaskLogger(this);
            Logger.SetLoggers(this.LogInfo, this.LogWarning, s => this.LogError(s));
        }

        [Required]
        public string SolutionDirectory { get; set; }

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
            VersionVariables result;
            if (!ExecuteCore.TryGetVersion(SolutionDirectory, out result, NoFetch, new Authentication()))
            {
                return;
            }

            WriteIntegrationParameters(BuildServerList.GetApplicableBuildServers(), result);
        }

        void WriteIntegrationParameters(IEnumerable<IBuildServer> applicableBuildServers, VersionVariables variables)
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