namespace GitVersionTask
{
    using System.Collections.Generic;
    using GitVersion;

    public class WriteVersionInfoToBuildLog : GitVersionTaskBase
    {
        protected override void InnerExecute()
        {
            if (GetVersionVariables(out var versionVariables)) return;

            WriteIntegrationParameters(BuildServerList.GetApplicableBuildServers(), versionVariables);
        }

        private void WriteIntegrationParameters(IEnumerable<IBuildServer> applicableBuildServers, VersionVariables versionVariables)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                LogInfo($"Executing GenerateSetVersionMessage for '{buildServer.GetType().Name}'.");
                LogInfo(buildServer.GenerateSetVersionMessage(versionVariables));
                LogInfo($"Executing GenerateBuildLogOutput for '{buildServer.GetType().Name}'.");
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, versionVariables))
                {
                    LogInfo(buildParameter);
                }
            }
        }
    }
}
