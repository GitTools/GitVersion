namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class FeatureVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch FeatureBranch;

        public SemanticVersion FindVersion()
        {
            var firstCommitOnBranch = Repository.Refs.Log(FeatureBranch.CanonicalName).Last();

            if (firstCommitOnBranch.To == Commit.Id) //no commits on branch. use develop approach
            {
                return new DevelopVersionFinder
                       {
                           Commit = Commit, 
                           Repository = Repository
                       }
                    .FindVersion();
            }


            var masterBranch = Repository.MasterBranch();

            var versionFromMaster = masterBranch.GetVersionPriorTo(Commit.When());

            var versionString = MergeMessageParser.GetVersionFromMergeCommit(versionFromMaster.Version);
            var version = SemanticVersion.FromMajorMinorPatch(versionString);
            version.Minor++;
            version.Patch = 0;
            version.Stage = Stage.Feature;
            version.Suffix = firstCommitOnBranch.To.Sha.Substring(0,8);
            version.PreRelease = 0;
            
            return version;
        }
    }
}