namespace GitFlowVersion
{
    using LibGit2Sharp;

    public class VersionForRepositoryFinder
    {
        public SemanticVersion SemanticVersion;

        public VersionAndBranch GetVersion(Repository repository, Branch branch)
        {
            var gitFlowVersionFinder = new GitFlowVersionFinder
                                       {
                                           Branch = branch,
                                           Commit = branch.Tip,
                                           Repository = repository
                                       };
            return gitFlowVersionFinder.FindVersion();
        }
    }
}
