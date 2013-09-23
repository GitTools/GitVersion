namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class DevelopVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;

        public VersionAndBranch FindVersion()
        {
            var version = GetSemanticVersion();

            version.Minor++;
            version.Patch = 0;

            version.Stability = Stability.Unstable;

            return new VersionAndBranch
                   {
                       BranchType = BranchType.Develop,
                       BranchName = "develop",
                       Sha = Commit.Sha,
                       Version = version
                   };
        }

        SemanticVersion GetSemanticVersion()
        {
            var versionOnMasterFinder = new VersionOnMasterFinder
                                        {
                                            Repository = Repository,
                                        };
            var versionFromMaster = versionOnMasterFinder.Execute(Commit.When());
            var version = versionFromMaster.Version;
            var developBranch = Repository.DevelopBranch();
            version.PreReleaseNumber = developBranch.Commits
                                              .SkipWhile(x => x != Commit)
                                              .TakeWhile(x => x.When() >= versionFromMaster.Timestamp)
                                              .Count();
            return version;
        }


    }
}