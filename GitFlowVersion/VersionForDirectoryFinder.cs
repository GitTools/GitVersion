namespace GitFlowVersion
{
    using LibGit2Sharp;

    public class VersionForRepositoryFinder
    {
        public VersionAndBranch GetVersion(Repository repository)
        {
            var gitFlowVersionFinder = new GitVersionFinder();
            return gitFlowVersionFinder.FindVersion(new GitVersionContext
            {
                CurrentBranch = repository.Head,
                Repository = repository
            });
        }
    }
}
