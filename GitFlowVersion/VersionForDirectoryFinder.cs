namespace GitFlowVersion
{
    using LibGit2Sharp;

    public class VersionForRepositoryFinder
    {
        public SemanticVersion SemanticVersion;

        public VersionAndBranch GetVersion(Repository repository)
        {
            var gitFlowVersionFinder = new GitFlowVersionFinder();
            return gitFlowVersionFinder.FindVersion(new GitFlowVersionContext
            {
                CurrentBranch = repository.Head,
                Tip = repository.Head.Tip,
                Repository = repository
            });
        }
    }
}
