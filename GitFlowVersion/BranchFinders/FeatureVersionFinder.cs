namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class FeatureVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch FeatureBranch;

        public VersionAndBranch FindVersion()
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

            
            var versionFromMaster = Repository.MasterVersionPriorTo(Commit.When());
            var version = SemanticVersion.FromMajorMinorPatch(versionFromMaster.Version);
            version.Minor++;
            version.Patch = 0;
            version.Stability = Stability.Unstable;
            version.PreReleaseNumber = 0;
            version.Suffix = firstCommitOnBranch.To.Sha.Substring(0, 8);

            return new VersionAndBranch
            {
                BranchType = BranchType.Feature,
                BranchName = FeatureBranch.Name,
                Sha = Commit.Sha,
                Version = version

            };
        }
    }
}