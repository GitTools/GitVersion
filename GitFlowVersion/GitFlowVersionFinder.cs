namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class GitFlowVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch Branch;

        public VersionAndBranch FindVersion()
        {
            EnsureMainTopologyConstraints();

            if (Branch.IsMaster())
            {
                return new MasterVersionFinder
                       {
                           Commit = Commit,
                           Repository = Repository
                       }.FindVersion();
            }

            if (Branch.IsHotfix())
            {
                return new HotfixVersionFinder
                       {
                           Commit = Commit,
                           HotfixBranch = Branch,
                           Repository = Repository
                       }.FindVersion();
            }

            if (Branch.IsRelease())
            {
                return new ReleaseVersionFinder
                       {
                           Commit = Commit,
                           Repository = Repository,
                           ReleaseBranch = Branch,
                       }.FindVersion();
            }

            if (Branch.IsDevelop())
            {
                return new DevelopVersionFinder
                       {
                           Commit = Commit,
                           Repository = Repository
                       }.FindVersion();
            }

            if (Branch.IsPullRequest())
            {
                return new PullVersionFinder
                       {
                           Commit = Commit,
                           Repository = Repository,
                           PullBranch = Branch
                       }.FindVersion();
            }
            if (Branch.IsFeature())
            {
                return new FeatureVersionFinder
                       {
                           Commit = Commit,
                           Repository = Repository,
                           FeatureBranch = Branch
                       }.FindVersion();
            }

            throw new ErrorException("Branch '{0}' doesn't respect the GitFlowVersion naming convention.");
        }

        void EnsureMainTopologyConstraints()
        {
            EnsureLocalBranchExists("master");
            EnsureLocalBranchExists("develop");
            EnsureHeadIsNotDetached();
        }

        void EnsureHeadIsNotDetached()
        {
            if (!Branch.CanonicalName.Equals("(no branch)", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var message = string.Format("It looks like the branch being examined is a detached Head pointing to commit '{0}'. Without a proper branch name GitFlowVersion cannot determine the build version.", Branch.Tip.Id.ToString(7));
            throw new ErrorException(message);
        }

        void EnsureLocalBranchExists(string branchName)
        {
            if (Repository.FindBranch(branchName) != null)
            {
                return;
            }

            var existingBranches = string.Format("'{0}'", string.Join("', '", Repository.Branches.Select(x=>x.CanonicalName)));
            throw new ErrorException(string.Format("This repository doesn't contain a branch named '{0}'. Please create one. Existing branches: {1}", branchName, existingBranches));
        }
    }
}
