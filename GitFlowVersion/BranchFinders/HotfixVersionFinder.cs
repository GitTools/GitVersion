namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class HotfixVersionFinder
    {
        public Commit Commit;
        public Branch HotfixBranch;
        public Branch MasterBranch;

        public VersionInformation FindVersion()
        {
            var version = VersionInformation.FromMajorMinorPatch(HotfixBranch.Name.Replace("hotfix-", ""));
            version.Stability = Stability.Beta;
            version.BranchType = BranchType.Hotfix;
            version.BranchName = HotfixBranch.Name;
            version.Sha = Commit.Sha;

            version.PreReleaseNumber = HotfixBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(MasterBranch))
                .Count();
            return version;
        }
    }
}