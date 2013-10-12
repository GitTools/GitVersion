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
            SemanticVersion versionFromBranchName;
            if (!SemanticVersionParser.TryParse(versionString, out versionFromBranchName))
            {
                var message = string.Format("Could not parse '{0}' into a version", ReleaseBranch.Name);
                throw new ErrorException(message);
            }

            if (versionFromBranchName.PreReleasePartOne != null)
            {
                throw new ErrorException("Release branches cannot contain a pre-release number");
            }
            if (versionFromBranchName.Stability != null)
            {
                throw new ErrorException("Release branches cannot contain a stability");
            }

            var versionFromFirstTag = GetVersionFromFirstTag(versionFromBranchName);
            return new VersionAndBranch
                        {
                            BranchType = BranchType.Release,
                            BranchName = ReleaseBranch.Name,
                            Sha = Commit.Sha,
                            Version = versionFromFirstTag,
                        };
        }

        SemanticVersion GetVersionFromFirstTag(SemanticVersion versionFromBranchName)
        {
            var count = 0;
            foreach (var c in ReleaseBranch
                .Commits
                .SkipWhile(x => x != Commit))
            {
                var versionFromTag = Repository.SemVerTag(c);
                if (versionFromTag == null)
                {
                    count++;
                    continue;
                }
                if (versionFromBranchName.Major == versionFromTag.Major &&
                    versionFromBranchName.Minor == versionFromTag.Minor &&
                    versionFromBranchName.Patch == versionFromTag.Patch)
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
                    return versionFromTag;
                }
                count++;
            }
            throw new Exception(string.Format("There must be a tag on a release branch with a version the same as the version from the branch name i.e. {0}.{1}.{2}", versionFromBranchName.Major, versionFromBranchName.Minor, versionFromBranchName.Patch));
   
        }
    }
}