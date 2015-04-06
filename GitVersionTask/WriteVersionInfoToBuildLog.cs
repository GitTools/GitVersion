namespace GitVersionTask
{
    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
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
            Tuple<CachedVersion, GitVersionContext> result;
            var gitDirectory = GitDirFinder.TreeWalkForDotGitDir(SolutionDirectory);

            if (gitDirectory == null)
                throw new DirectoryNotFoundException(string.Format("Unable to locate a git repository in \"{0}\". Make sure that the solution is located in a git controlled folder. If you are using continous integration make sure that the sources are checked out on the build agent.", SolutionDirectory));

            var configuration = ConfigurationProvider.Provide(gitDirectory, fileSystem);
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out result, configuration, NoFetch))
            {
                return;
            }

            var authentication = new Authentication();

            var cachedVersion = result.Item1;
            var gitVersionContext = result.Item2;
            var config = gitVersionContext.Configuration;
            var assemblyVersioningScheme = config.AssemblyVersioningScheme;
            var versioningMode = config.VersioningMode;

            var variablesFor = VariableProvider.GetVariablesFor(
                cachedVersion.SemanticVersion, assemblyVersioningScheme, versioningMode,
                config.ContinuousDeploymentFallbackTag,
                gitVersionContext.IsCurrentCommitTagged);
            WriteIntegrationParameters(cachedVersion, BuildServerList.GetApplicableBuildServers(authentication), variablesFor);
        }

        public void WriteIntegrationParameters(CachedVersion cachedVersion, IEnumerable<IBuildServer> applicableBuildServers, VersionVariables variables)
        {
            foreach (var buildServer in applicableBuildServers)
            {
                logger.LogInfo(string.Format("Executing GenerateSetVersionMessage for '{0}'.", buildServer.GetType().Name));
                logger.LogInfo(buildServer.GenerateSetVersionMessage(cachedVersion.SemanticVersion.ToString()));
                logger.LogInfo(string.Format("Executing GenerateBuildLogOutput for '{0}'.", buildServer.GetType().Name));
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, variables))
                {
                    logger.LogInfo(buildParameter);
                }
            }
        }
    }
}