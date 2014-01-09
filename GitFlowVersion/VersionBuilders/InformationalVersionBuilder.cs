namespace GitFlowVersion
{
    using System;

    public static class VersionInformationalBuilder
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
            var stability = version.Tag.InferStability();
            if (stability == Stability.Final)
            {
                return string.Format("{0} Sha:'{1}'", versionPrefix, versionAndBranch.Sha);
            }
            if (stability == Stability.ReleaseCandidate)
            {
                return string.Format("{0}-rc{1} Branch:'{2}' Sha:'{3}'", versionPrefix, GetPreRelease(version), versionAndBranch.BranchName, versionAndBranch.Sha);
            }
            return string.Format("{0}-{1}{2} Branch:'{3}' Sha:'{4}'", versionPrefix,stability.ToString().ToLowerInvariant(), GetPreRelease(version), versionAndBranch.BranchName, versionAndBranch.Sha);
        }

        static string GetPreRelease(SemanticVersion version)
        {
            if (!version.Tag.HasReleaseNumber())
            {
                throw new Exception("pre-release number is required");
            }
            if (version.PreReleasePartTwo == null)
            {
                return version.Tag.ReleaseNumber().ToString();
            }

            return string.Format("{0}.{1}", version.Tag.ReleaseNumber(), version.PreReleasePartTwo);
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
                if (version.Tag.InferStability() == Stability.ReleaseCandidate)
                {
                    return string.Format("{0}-rc{1}", versionPrefix, GetPreRelease(version));
                }
                return string.Format("{0}-{1}{2}", versionPrefix, version.Tag.InferStability().ToString().ToLowerInvariant(), GetPreRelease(version));
            }

            if (versionAndBranch.BranchType == BranchType.Hotfix)
            {
                return string.Format("{0}-{1}{2}", versionPrefix, version.Tag.InferStability().ToString().ToLowerInvariant(), GetPreRelease(version));
            }

            if (versionAndBranch.BranchType == BranchType.Master)
            {
                return versionPrefix;
            }

            throw new ErrorException(string.Format("Invalid branch type '{0}'.", versionAndBranch.BranchType));
        }

    }
}