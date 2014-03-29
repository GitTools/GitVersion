namespace GitVersion
{
    using System;

    public static class VersionInformationalBuilder
    {
        public static string ToLongString(this VersionAndBranch versionAndBranch)
        {
            var version = versionAndBranch.Version;
            var releaseInformation = ReleaseInformationCalculator.Calculate(versionAndBranch.BranchType, version.Tag);

            var versionString = versionAndBranch.ToShortString();

            if (versionAndBranch.BranchType != BranchType.Master && releaseInformation.Stability != Stability.Final)
            {
                versionString += String.Format(" Branch:'{0}'", versionAndBranch.BranchName);
            }

            return versionString + string.Format(" Sha:'{0}'", versionAndBranch.Sha);
        }


        public static string ToShortString(this VersionAndBranch versionAndBranch)
        {
            var version = versionAndBranch.Version;
            var releaseInformation = ReleaseInformationCalculator.Calculate(versionAndBranch.BranchType, version.Tag);
            var versionPrefix = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Patch);

            if (versionAndBranch.BranchType == BranchType.Feature)
            {
                var shortSha = versionAndBranch.Sha.Substring(0, 8);
                return string.Format("{0}-unstable.feature-{1}", versionPrefix, shortSha);
            }

            if (versionAndBranch.BranchType == BranchType.PullRequest)
            {
                return string.Format("{0}-unstable.pull-request-{1}", versionPrefix, GetPreRelease(version.PreReleasePartTwo, releaseInformation));
            }

            if (versionAndBranch.BranchType == BranchType.Release)
            {
                if (releaseInformation.Stability == Stability.ReleaseCandidate)
                {
                    return string.Format("{0}-rc{1}", versionPrefix, GetPreRelease(version.PreReleasePartTwo, releaseInformation));
                }
                return string.Format("{0}-{1}{2}", versionPrefix, releaseInformation.Stability.ToString().ToLowerInvariant(), GetPreRelease(version.PreReleasePartTwo, releaseInformation));
            }

            if (versionAndBranch.BranchType == BranchType.Master || releaseInformation.Stability == Stability.Final)
            {
                return versionPrefix;
            }


            if (versionAndBranch.BranchType == BranchType.Hotfix)
            {
                return string.Format("{0}-{1}{2}", versionPrefix, releaseInformation.Stability.ToString().ToLowerInvariant(), GetPreRelease(version.PreReleasePartTwo, releaseInformation));
            }


            return string.Format("{0}-{1}{2}", versionPrefix, releaseInformation.Stability.ToString().ToLowerInvariant(), GetPreRelease(version.PreReleasePartTwo, releaseInformation));
        }

        static string GetPreRelease(int? preReleasePartTwo, ReleaseInformation releaseInformation)
        {
            if (!releaseInformation.ReleaseNumber.HasValue)
            {
                throw new Exception("pre-release number is required");
            }
            if (preReleasePartTwo == null)
            {
                return releaseInformation.ReleaseNumber.ToString();
            }

            return string.Format("{0}.{1}", releaseInformation.ReleaseNumber, preReleasePartTwo);
        }


    }
}