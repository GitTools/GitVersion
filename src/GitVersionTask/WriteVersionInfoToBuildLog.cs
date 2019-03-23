namespace GitVersionTask
{
    using System;
    using System.Collections.Generic;
    using GitVersion;

    public static class WriteVersionInfoToBuildLog
    {

        public static Output Execute(
            Input input
            )
        {
            if ( !input.ValidateInput() )
            {
                throw new Exception( "Invalid input." );
            }

            var logger = new TaskLogger();
            Logger.SetLoggers( logger.LogInfo, logger.LogInfo, logger.LogWarning, s => logger.LogError( s ) );


            Output output = null;
            try
            {
                output = InnerExecute(logger, input);
            }
            catch (WarningException errorException)
            {
                logger.LogWarning(errorException.Message);
                output = new Output();
            }
            catch (Exception exception)
            {
                logger.LogError("Error occurred: " + exception);
                throw;
            }
            finally
            {
                Logger.Reset();
            }

            return output;
        }

        private static Output InnerExecute(
            TaskLogger logger,
            Input input
            )
        {
            var execute = GitVersionTaskBase.CreateExecuteCore();
            if (!execute.TryGetVersion(input.SolutionDirectory, out var result, input.NoFetch, new Authentication()))
            {
                return null;
            }

            WriteIntegrationParameters(logger, BuildServerList.GetApplicableBuildServers(), result);

            return new Output();
        }

        private static void WriteIntegrationParameters(TaskLogger logger, IEnumerable<IBuildServer> applicableBuildServers, VersionVariables variables)
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

        public sealed class Input
        {
            public string SolutionDirectory { get; set; }

            public bool NoFetch { get; set; }
        }

        public static Boolean ValidateInput(this Input input)
        {
            return !String.IsNullOrEmpty( input?.SolutionDirectory );
        }

        public sealed class Output
        {
            // No output for this task
        }
    }
}
