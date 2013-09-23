namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    public class VersionForRepositoryFinder
    {
        public SemanticVersion SemanticVersion;

        public VersionAndBranch GetVersion(Repository repository)
        {
            var gitFlowVersionFinder = new GitFlowVersionFinder
                                       {
                                           Branch = repository.Head,
                                           Commit = repository.Head.Commits.First(),
                                           Repository = repository
                                       };
            return gitFlowVersionFinder.FindVersion();
        }
    }
}