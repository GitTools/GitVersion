namespace GitVersionTask
{
    using System;
    using System.Collections.Generic;
    using GitVersion;
    using Microsoft.Build.Framework;

    public class WriteVersionInfoToBuildLog : GitVersionTaskBase
    {
        public WriteVersionInfoToBuildLog()
        {
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
                this.LogWarning(errorException.Message);
                return true;
            }
            catch (Exception exception)
            {
                this.LogError("Error occurred: " + exception);
                return false;
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
                this.LogInfo(string.Format("Executing GenerateSetVersionMessage for '{0}'.", buildServer.GetType().Name));
                this.LogInfo(buildServer.GenerateSetVersionMessage(variables));
                this.LogInfo(string.Format("Executing GenerateBuildLogOutput for '{0}'.", buildServer.GetType().Name));
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, variables))
                {
                    this.LogInfo(buildParameter);
                }
            }
        }
    }
}
