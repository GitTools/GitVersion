namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class FeatureVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch FeatureBranch;

        public VersionInformation FindVersion()
        {
            var firstCommitOnBranch = Repository.Refs.Log(FeatureBranch.CanonicalName).Last();

            if (firstCommitOnBranch.To == Commit.Id) //no commits on branch. use develop approach
            {
                var versionFromDevelopFinder = new DevelopVersionFinder
                                                  {
                                                      Commit = Commit, 
                                                      Repository = Repository
                                                  }
                    .FindVersion();
                versionFromDevelopFinder.BranchType = BranchType.Feature;
                versionFromDevelopFinder.BranchName = FeatureBranch.Name;
                return versionFromDevelopFinder;
            }


            var masterBranch = Repository.MasterBranch();

            var versionFromMaster = masterBranch.GetVersionPriorTo(Commit.When());

            var versionString = MergeMessageParser.GetVersionFromMergeCommit(versionFromMaster.Version);
            var version = VersionInformation.FromMajorMinorPatch(versionString);
            version.Minor++;
            version.Patch = 0;
            version.Stability = Stability.Unstable;
            version.BranchType = BranchType.Feature;
            version.Suffix = firstCommitOnBranch.To.Sha.Substring(0,8);
            version.PreReleaseNumber = 0;
            version.BranchName = FeatureBranch.Name;
            version.Sha = Commit.Sha;
            
            return version;
        }
    }
}