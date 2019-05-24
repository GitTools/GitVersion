namespace GitVersionTask
{
    using System.Collections.Generic;
    using GitVersion;

    public class WriteVersionInfoToBuildLog
    {
        public static Output Execute(Input input) => GitVersionTaskUtils.ExecuteGitVersionTask(input, InnerExecute);

        public sealed class Input : InputBase
        {
            // No additional inputs for this task
        }

        public sealed class Output
        {
            // No output for this task
        }

        private static Output InnerExecute(Input input, TaskLogger logger)
        {
            if (!GitVersionTaskUtils.GetVersionVariables(input, out var versionVariables))
            {
                return null;
            }

            WriteIntegrationParameters(logger, BuildServerList.GetApplicableBuildServers(), versionVariables);

            return new Output();
        }

        private static void WriteIntegrationParameters(TaskLogger logger, IEnumerable<IBuildServer> applicableBuildServers, VersionVariables versionVariables)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                logger.LogInfo($"Executing GenerateSetVersionMessage for '{ buildServer.GetType().Name }'.");
                logger.LogInfo(buildServer.GenerateSetVersionMessage(versionVariables));
                logger.LogInfo($"Executing GenerateBuildLogOutput for '{ buildServer.GetType().Name }'.");
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, versionVariables))
                {
                    logger.LogInfo(buildParameter);
                }
            }
        }
    }
}
