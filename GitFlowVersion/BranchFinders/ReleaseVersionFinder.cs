namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class ReleaseVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch ReleaseBranch;

        public VersionAndBranch FindVersion()
        {
            var versionString = ReleaseBranch.GetReleaseSuffix();

            int patch;
            int minor;
            int major;
            ShortVersionParser.Parse(versionString, out major, out minor, out patch);

            var count = 0;
            foreach (var c in ReleaseBranch
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
                            throw new Exception("If a stability is defined on a release branch the pre-release number must also be defined.");
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
                        BranchType = BranchType.Release,
                        BranchName = ReleaseBranch.Name,
                        Sha = Commit.Sha,
                        Version = versionFromTag,
                    };
                }
                count++;
            }
            var message = string.Format("There must be a tag on a release branch with a version the same as the version from the branch name i.e. {0}.{1}.{2}", major, minor, patch);
            throw new Exception(message);

        }
    }
}