namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    abstract class DevelopBasedVersionFinderBase
    {
        protected VersionAndBranch FindVersion(
            GitFlowVersionContext context,
            BranchType branchType)
        {
            var ancestor = FindCommonAncestorWithDevelop(context.Repository, context.CurrentBranch, branchType);

            if (!IsThereAnyCommitOnTheBranch(context.Repository, context.CurrentBranch))
            {
                var developVersionFinder = new DevelopVersionFinder();
                var versionFromDevelopFinder = developVersionFinder.FindVersion(context);
                versionFromDevelopFinder.BranchType = branchType;
                versionFromDevelopFinder.BranchName = context.CurrentBranch.Name;
                return versionFromDevelopFinder;
            }

            var versionOnMasterFinder = new VersionOnMasterFinder();
            var versionFromMaster = versionOnMasterFinder.Execute(context, context.Tip.Committer.When);

            return new VersionAndBranch
            {
                BranchType = branchType,
                BranchName = context.CurrentBranch.Name,
                Sha = context.Tip.Sha,
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
