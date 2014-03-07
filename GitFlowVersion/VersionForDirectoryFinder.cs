namespace GitFlowVersion
{
    using LibGit2Sharp;

    public class VersionForRepositoryFinder
    {
        public VersionAndBranchAndDate GetVersion(Repository repository)
        {
            var gitFlowVersionFinder = new GitVersionFinder();
            var vab =  gitFlowVersionFinder.FindVersion(new GitVersionContext
            {
                CurrentBranch = repository.Head,
                Repository = repository
            });

            var rd = ReleaseDateFinder.Execute(repository, vab);

            return new VersionAndBranchAndDate(vab, rd);
        }
    }
}
