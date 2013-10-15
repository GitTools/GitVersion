namespace GitFlowVersion
{
    using System;

    public static class VersionInformationalConverter
    {
        public static string ToLongString(this VersionAndBranch versionAndBranch)
        {
            var version = versionAndBranch.Version;
            var versionPrefix = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Patch);

            if (versionAndBranch.BranchType == BranchType.Feature)
            {
                var shortSha = versionAndBranch.Sha.Substring(0, 8);
                return string.Format("{0}-unstable.feature-{1} Branch:'{2}' Sha:'{3}'", versionPrefix, shortSha, versionAndBranch.BranchName, versionAndBranch.Sha);
            }

            if (versionAndBranch.BranchType == BranchType.PullRequest)
            {
                return string.Format("{0}-unstable.pull-request-{1} Branch:'{2}' Sha:'{3}'", versionPrefix, GetPreRelease(version), versionAndBranch.BranchName, versionAndBranch.Sha);
            }


            if (versionAndBranch.BranchType == BranchType.Master)
            {
                return string.Format("{0} Sha:'{1}'", versionPrefix, versionAndBranch.Sha);
            }


            //else Hotfix, Develop or Release
            if (version.Stability == Stability.ReleaseCandidate)
            {
                return string.Format("{0}-rc{1} Branch:'{2}' Sha:'{3}'", versionPrefix, GetPreRelease(version), versionAndBranch.BranchName, versionAndBranch.Sha);
            }
            return string.Format("{0}-{1}{2} Branch:'{3}' Sha:'{4}'", versionPrefix,version.Stability.ToString().ToLowerInvariant(), GetPreRelease(version), versionAndBranch.BranchName, versionAndBranch.Sha);
        }

        static string GetPreRelease(SemanticVersion version)
        {
            if (version.PreReleasePartOne == null)
            {
                throw new Exception("pre-release number is required");
            }
            if (version.PreReleasePartTwo == null)
            {
                return version.PreReleasePartOne.ToString();
            }

            return string.Format("{0}.{1}", version.PreReleasePartOne, version.PreReleasePartTwo);
        }

        public static string ToShortString(this VersionAndBranch versionAndBranch)
        {
            var version = versionAndBranch.Version;
            var versionPrefix = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Patch);

            if (versionAndBranch.BranchType == BranchType.Feature)
            {
                var shortSha = versionAndBranch.Sha.Substring(0, 8);
                return string.Format("{0}-unstable.feature-{1}", versionPrefix, shortSha);
            }

            if (versionAndBranch.BranchType == BranchType.PullRequest)
            {
                return string.Format("{0}-unstable.pull-request-{1}", versionPrefix, GetPreRelease(version));
            }

            if (versionAndBranch.BranchType == BranchType.Develop)
            {
                return string.Format("{0}-unstable{1}", versionPrefix, GetPreRelease(version));
            }

            if (versionAndBranch.BranchType == BranchType.Release)
            {
                if (version.Stability == Stability.ReleaseCandidate)
                {
                    return string.Format("{0}-rc{1}", versionPrefix, GetPreRelease(version));
                }
                return string.Format("{0}-beta{1}", versionPrefix, GetPreRelease(version));
            }

            if (versionAndBranch.BranchType == BranchType.Hotfix)
            {
                return string.Format("{0}-beta{1}", versionPrefix, GetPreRelease(version));
            }

            if (versionAndBranch.BranchType == BranchType.Master)
            {
                return versionPrefix;
            }

            throw new ErrorException(string.Format("Invalid branch type '{0}'.", versionAndBranch.BranchType));
        }
    }
}