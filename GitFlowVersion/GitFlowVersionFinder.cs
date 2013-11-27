namespace GitFlowVersion
{
    using System;
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
            return new FeatureVersionFinder
                   {
                       Commit = Commit,
                       Repository = Repository,
                       FeatureBranch = Branch
                   }.FindVersion();
        }

        private void EnsureMainTopologyConstraints()
        {
            EnsureLocalBranchExists("master");
            EnsureLocalBranchExists("develop");
            EnsureHeadIsNotDetached();
        }

        private void EnsureHeadIsNotDetached()
        {
            if (!Branch.CanonicalName.Equals("(no branch)", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new ErrorException(
                string.Format("It looks like the branch being examined is a detached Head pointing to commit '{0}'. "
                + "Without a proper branch name GITFlowVersion cannot determine the build version."
                , Branch.Tip.Id.ToString(7)));
        }

        private void EnsureLocalBranchExists(string branchName)
        {
            if (Repository.Branches[branchName] != null)
            {
                return;
            }

            throw new ErrorException(string.Format("This repository doesn't contain a branch named '{0}'. Please create one.", branchName));
        }
    }
}
