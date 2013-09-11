namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class DevelopVersionFinder
    {
        public Commit Commit;
        public Repository Repository;

        public SemanticVersion FindVersion()
        {
            var version = GetSemanticVersion();

            version.Minor++;
            version.Patch = 0;

            version.Stage = Stage.Unstable;

            return version;
        }

        SemanticVersion GetSemanticVersion()
        {
            var masterBranch = Repository.MasterBranch();

            var versionFromMaster = masterBranch.GetVersionPriorTo(Commit.When());
           
            var version = SemanticVersion.FromMajorMinorPatch(versionFromMaster.Version);

            var developBranch = Repository.DevelopBranch();
            version.PreRelease = developBranch.Commits
                                              .SkipWhile(x => x != Commit)
                                              .TakeWhile(x => x.When() >= versionFromMaster.Timestamp)
                                              .Count();
            return version;
        }


    }
}