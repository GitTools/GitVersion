namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class DevelopVersionFinder
    {
        public Commit Commit;
        public Repository Repository;

        public VersionInformation FindVersion()
        {
            var version = GetSemanticVersion();

            version.Minor++;
            version.Patch = 0;

            version.Stability = Stability.Unstable;
            version.BranchType = BranchType.Develop;

            version.BranchName = "develop";
            version.Sha = Commit.Sha;
            return version;
        }

        VersionInformation GetSemanticVersion()
        {
            var masterBranch = Repository.MasterBranch();

            var versionFromMaster = masterBranch.GetVersionPriorTo(Commit.When());
           
            var version = VersionInformation.FromMajorMinorPatch(versionFromMaster.Version);

            var developBranch = Repository.DevelopBranch();
            version.PreReleaseNumber = developBranch.Commits
                                              .SkipWhile(x => x != Commit)
                                              .TakeWhile(x => x.When() >= versionFromMaster.Timestamp)
                                              .Count();
            return version;
        }


    }
}