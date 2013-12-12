namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class DevelopBasedVersionFinderBase
    {
        public VersionAndBranch FindVersion(
            IRepository repo,
            Branch branch,
            Commit commit,
            BranchType branchType)
        {
            var ancestor = FindCommonAncestorWithDevelop(repo, branch, branchType);

            if (!IsThereAnyCommitOnTheBranch(repo, branch))
            {
                var developVersionFinder = new DevelopVersionFinder
                {
                    Commit = commit,
                    Repository = repo
                };
                var versionFromDevelopFinder = developVersionFinder.FindVersion();
                versionFromDevelopFinder.BranchType = branchType;
                versionFromDevelopFinder.BranchName = branch.Name;
                return versionFromDevelopFinder;
            }

            var versionOnMasterFinder = new VersionOnMasterFinder
            {
                Repository = repo,
                OlderThan = commit.When()
            };
            var versionFromMaster = versionOnMasterFinder.Execute();

            return new VersionAndBranch
            {
                BranchType = branchType,
                BranchName = branch.Name,
                Sha = commit.Sha,
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

        Commit FindCommonAncestorWithDevelop(IRepository repo, Branch branch, BranchType branchType)
        {
            var ancestor = repo.Commits.FindCommonAncestor(
                repo.FindBranch("develop").Tip,
                branch.Tip);

            if (ancestor != null)
            {
                return ancestor;
            }

            throw new ErrorException(
                string.Format("A {0} branch is expected to branch off of 'develop'. "
                              + "However, branch 'develop' and '{1}' do not share a common ancestor."
                    , branchType, branch.Name));
        }

        public bool IsThereAnyCommitOnTheBranch(IRepository repo, Branch branch)
        {
            var filter = new CommitFilter
            {
                Since = branch,
                Until = repo.FindBranch("develop")
            };

            var commits = repo.Commits.QueryBy(filter);

            if (!commits.Any())
            {
                return false;
            }

            return true;
        }
    }
}
