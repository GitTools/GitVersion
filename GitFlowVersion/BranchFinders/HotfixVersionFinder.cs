namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class HotfixVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch HotfixBranch;
        public Branch MasterBranch;

        public SemanticVersion FindVersion()
        {
            var version = SemanticVersion.FromMajorMinorPatch(HotfixBranch.Name.Replace("hotfix-", ""));
            version.Stage = Stage.Beta;

            version.PreRelease = HotfixBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(MasterBranch))
                .Count();
            return version;
        }
    }
}