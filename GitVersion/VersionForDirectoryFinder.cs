namespace GitVersion
{
    using LibGit2Sharp;

    public class VersionForRepositoryFinder
    {
        public SemanticVersion SemanticVersion;

        public VersionAndBranch GetVersion(Repository repository)
        {
            var gitVersionFinder = new GitVersionFinder();
            return gitVersionFinder.FindVersion(new GitVersionContext
            {
                CurrentBranch = repository.Head,
                Repository = repository
            });
        }
    }
}
