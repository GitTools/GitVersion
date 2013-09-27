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
            var versionString = HotfixBranch.Name.Replace("hotfix-", "");
            SemanticVersion version;
            if (!SemanticVersionParser.TryParse(versionString, out  version))
            {
                var message = string.Format("Could not parse '{0}' into a version", HotfixBranch.Name);
                throw new ErrorException(message);
            }

            if (version.PreReleaseNumber != null)
            {
                var message = string.Format("Hotfix branch name is invalid '{0}'. PreReleaseNumber not allowed as part of hotfix branch name", HotfixBranch.Name);
                throw new ErrorException(message);
            }
            if (version.Stability != null)
            {
                var message = string.Format("Hotfix branch name is invalid '{0}'. Stability not allowed as part of hotfix branch name", HotfixBranch.Name);
                throw new ErrorException(message);
            }
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