namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class FeatureVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch FeatureBranch;
        internal Func<ObjectId> FindFirstCommitOnBranchFunc;

        public FeatureVersionFinder()
        {
            FindFirstCommitOnBranchFunc = FindFirstCommitOnBranch;
        }

        public VersionAndBranch FindVersion()
        {
            var firstCommitOnBranch = FindFirstCommitOnBranchFunc();

            if (firstCommitOnBranch == Commit.Id) //no commits on branch. use develop approach
            {
                var developVersionFinder = new DevelopVersionFinder
                {
                    Commit = Commit,
                    Repository = Repository
                };
                var versionFromDevelopFinder = developVersionFinder.FindVersion();
                versionFromDevelopFinder.BranchType = BranchType.Feature;
                versionFromDevelopFinder.BranchName = FeatureBranch.Name;
                return versionFromDevelopFinder;
            }

            var versionOnMasterFinder = new VersionOnMasterFinder
            {
                Repository = Repository,
                OlderThan = Commit.When()
            };
            var versionFromMaster = versionOnMasterFinder.Execute();

            return new VersionAndBranch
            {
                BranchType = BranchType.Feature,
                BranchName = FeatureBranch.Name,
                Sha = Commit.Sha,
                Version = new SemanticVersion
                {
                    Major = versionFromMaster.Major,
                    Minor = versionFromMaster.Minor + 1,
                    Patch = 0,
                    Stability = Stability.Unstable,
                    PreReleasePartOne = 0,
                    Suffix = firstCommitOnBranch.Prefix()
                }
            };
        }


        public ObjectId FindFirstCommitOnBranch()
        {
            return Repository.Refs.Log(FeatureBranch.CanonicalName).Last().To;
        }
    }

}