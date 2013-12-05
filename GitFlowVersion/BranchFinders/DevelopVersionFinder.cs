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
                                            OlderThan = Commit.When()
                                        };
            var versionFromMaster = versionOnMasterFinder.Execute();

            var developBranch = Repository.FindBranch("develop");
            var preReleasePartOne = developBranch.Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => x.When() >= versionFromMaster.Timestamp)
                .Count();
            return new SemanticVersion
            {
                Major = versionFromMaster.Major,
                Minor = versionFromMaster.Minor,
                PreReleasePartOne = preReleasePartOne
            };
        }


    }
}