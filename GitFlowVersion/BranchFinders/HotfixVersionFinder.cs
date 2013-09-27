namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class HotfixVersionFinder
    {
        public Commit Commit;
        public Branch HotfixBranch;
        public IRepository Repository;
        public Func<Commit, bool> IsOnMasterBranchFunc;

        public HotfixVersionFinder()
        {
            IsOnMasterBranchFunc = x =>
                {
                    var masterBranch = Repository.MasterBranch();
                    return Repository.IsOnBranch(masterBranch, x);
                }; 
        }

        public VersionAndBranch FindVersion()
        {
            var version = SemanticVersionParser.FromMajorMinorPatch(HotfixBranch.Name.Replace("hotfix-", ""));
            version.Stability = Stability.Beta;

            version.PreReleaseNumber = HotfixBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !IsOnMasterBranchFunc(x))
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