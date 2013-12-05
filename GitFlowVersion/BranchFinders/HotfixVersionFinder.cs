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

        public VersionAndBranch FindVersion()
        {
            EnsureHotfixBranchShareACommonAncestorWithMaster();

            var versionString = HotfixBranch.GetHotfixSuffix();

            int patch;
            int minor;
            int major;
            ShortVersionParser.Parse(versionString, out major, out minor, out patch);

            var count = 0;
            foreach (var c in HotfixBranch
                .Commits
                .SkipWhile(x => x != Commit))
            {
                var versionFromTag = Repository.NewestSemVerTag(c);
                if (versionFromTag == null)
                {
                    count++;
                    continue;
                }
                if (major == versionFromTag.Major &&
                    minor == versionFromTag.Minor &&
                    patch == versionFromTag.Patch)
                {
                    if (versionFromTag.Stability != null)
                    {
                        if (versionFromTag.PreReleasePartOne == null)
                        {
                            throw new Exception("If a stability is defined on a hotfix branch the pre-release number must also be defined.");
                        }
                        if (versionFromTag.PreReleasePartTwo != null)
                        {
                            throw new Exception("pre release part two is reserved for commit increments.");
                        }
                        if (count != 0)
                        {
                            versionFromTag.PreReleasePartTwo = count;
                        }
                    }
                    return new VersionAndBranch
                    {
                        BranchType = BranchType.Hotfix,
                        BranchName = HotfixBranch.Name,
                        Sha = Commit.Sha,
                        Version = versionFromTag,
                    };
                }
                count++;
            }
            var message = string.Format("There must be a tag on a hotfix branch with a version the same as the version from the branch name i.e. {0}.{1}.{2}", major, minor, patch);
            throw new Exception(message);

        }

        private void EnsureHotfixBranchShareACommonAncestorWithMaster()
        {
            var ancestor = Repository.Commits.FindCommonAncestor(
                Repository.FindBranch("master").Tip,
                HotfixBranch.Tip);

            if (ancestor != null)
            {
                return;
            }

            throw new ErrorException(
                "A hotfix branch is expected to branch off of 'master'. "
                + string.Format("However, branch 'master' and '{0}' do not share a common ancestor."
                , HotfixBranch.Name));
        }
    }
}
