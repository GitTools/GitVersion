namespace GitVersion
{
    using System;
    using GitVersion.Helpers;

    public static class ExecuteCore
    {
        public static VersionVariables ExecuteGitVersion(IFileSystem fileSystem, string targetUrl, string dynamicRepositoryLocation, Authentication authentication, string targetBranch, bool noFetch, string workingDirectory, string commitId)
        {
            var gitPreparer = new GitPreparer(targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, workingDirectory);
            gitPreparer.InitialiseDynamicRepositoryIfNeeded();
            var dotGitDirectory = gitPreparer.GetDotGitDirectory();
            var projectRoot = gitPreparer.GetProjectRootDirectory();
            if (string.IsNullOrEmpty(dotGitDirectory) || string.IsNullOrEmpty(projectRoot))
            {
                // TODO Link to wiki article
                throw new Exception(string.Format("Failed to prepare or find the .git directory in path '{0}'.", workingDirectory));
            }

            foreach (var buildServer in BuildServerList.GetApplicableBuildServers(authentication))
            {
                buildServer.PerformPreProcessingSteps(dotGitDirectory, noFetch);
            }
            VersionVariables variables;
            var versionFinder = new GitVersionFinder();
            var configuration = ConfigurationProvider.Provide(projectRoot, fileSystem);

            using (var repo = RepositoryLoader.GetRepo(dotGitDirectory))
            {
                var gitVersionContext = new GitVersionContext(repo, configuration, commitId: commitId);
                var semanticVersion = versionFinder.FindVersion(gitVersionContext);
                var config = gitVersionContext.Configuration;
                variables = VariableProvider.GetVariablesFor(semanticVersion, config.AssemblyVersioningScheme, config.VersioningMode, config.ContinuousDeploymentFallbackTag, gitVersionContext.IsCurrentCommitTagged);
            }

            return variables;
        }
    }
}