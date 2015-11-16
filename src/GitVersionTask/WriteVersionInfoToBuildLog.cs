namespace GitVersionTask
{
    using System;
    using System.Collections.Generic;

    using GitVersion;

    using Microsoft.Build.Framework;

    public class WriteVersionInfoToBuildLog : GitVersionTaskBase
    {
        readonly TaskLogger logger;


        public WriteVersionInfoToBuildLog()
        {
            this.logger = new TaskLogger(this);
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
                this.logger.LogWarning(errorException.Message);
                return true;
            }
            catch (Exception exception)
            {
                this.logger.LogError("Error occurred: " + exception);
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
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out result, NoFetch, new Authentication()))
            {
                return;
            }

            WriteIntegrationParameters(BuildServerList.GetApplicableBuildServers(), result);
        }


        void WriteIntegrationParameters(IEnumerable<IBuildServer> applicableBuildServers, VersionVariables variables)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                this.logger.LogInfo(string.Format("Executing GenerateSetVersionMessage for '{0}'.", buildServer.GetType().Name));
                this.logger.LogInfo(buildServer.GenerateSetVersionMessage(variables));
                this.logger.LogInfo(string.Format("Executing GenerateBuildLogOutput for '{0}'.", buildServer.GetType().Name));
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, variables))
                {
                    this.logger.LogInfo(buildParameter);
                }
            }
        }
    }
}