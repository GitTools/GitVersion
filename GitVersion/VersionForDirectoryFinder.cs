namespace GitVersion
{
    using LibGit2Sharp;

    public class VersionForRepositoryFinder
    {
        public VersionAndBranchAndDate GetVersion(Repository repository)
        {
            var gitVersionFinder = new GitVersionFinder();
            var vab =  gitVersionFinder.FindVersion(new GitVersionContext
            {
                CurrentBranch = repository.Head,
                Repository = repository
            });

            var rd = ReleaseDateFinder.Execute(repository, vab);

            return new VersionAndBranchAndDate(vab, rd);
        }
    }
}
