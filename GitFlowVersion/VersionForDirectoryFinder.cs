namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    public class VersionForRepositoryFinder
    {
        public VersionInformation VersionInformation;

        public VersionInformation GetVersion(Repository repository)
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