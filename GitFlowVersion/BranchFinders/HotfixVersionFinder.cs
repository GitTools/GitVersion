namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class HotfixVersionFinder
    {
        public Commit Commit;
        public Branch HotfixBranch;
        public Branch MasterBranch;

        public VersionAndBranch FindVersion()
        {
            var version = SemanticVersion.FromMajorMinorPatch(HotfixBranch.Name.Replace("hotfix-", ""));
            version.Stability = Stability.Beta;

            version.PreReleaseNumber = HotfixBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(MasterBranch))
                .Count();

            return new VersionAndBranch
            {
                BranchType = BranchType.Hotfix,
                BranchName = HotfixBranch.Name,
                Sha = Commit.Sha,
                Version = version
            };
        }
    }
}