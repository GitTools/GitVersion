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
            var ancestor = FindCommonAncestorWithDevelop();

            var firstCommitOnBranch = FindFirstCommitOnBranchFunc();

            if (firstCommitOnBranch == null) //no commits on branch. use develop approach
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
                    Suffix = ancestor.Prefix()
                }
            };
        }

        private Commit FindCommonAncestorWithDevelop()
        {
            var ancestor = Repository.Commits.FindCommonAncestor(
                Repository.Branches["develop"].Tip,
                FeatureBranch.Tip);

            if (ancestor != null)
            {
                return ancestor;
            }

            throw new ErrorException(
                "A feature branch is expected to branch off of 'develop'. " 
                + string.Format("However, branch 'develop' and '{0}' do not share a common ancestor."
                , FeatureBranch.Name));
        }


        public ObjectId FindFirstCommitOnBranch()
        {
            var filter = new CommitFilter
                         {
                             Since = FeatureBranch,
                             Until = Repository.Branches["develop"]
                         };

            var commits = Repository.Commits.QueryBy(filter).ToList();

            if (commits.Count == 0)
            {
                return null;
            }

            return commits.Last().Id;
        }
    }

}