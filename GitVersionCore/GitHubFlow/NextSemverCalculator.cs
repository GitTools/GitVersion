namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class NextSemverCalculator
    {
        NextVersionTxtFileFinder nextVersionTxtFileFinder;
        LastTaggedReleaseFinder lastTaggedReleaseFinder;
        OtherBranchVersionFinder unknownBranchFinder;
        GitVersionContext context;
        MergedBranchesWithVersionFinder mergedBranchesWithVersionFinder;

        public NextSemverCalculator(
            NextVersionTxtFileFinder nextVersionTxtFileFinder,
            LastTaggedReleaseFinder lastTaggedReleaseFinder,
            GitVersionContext context)
        {
            this.nextVersionTxtFileFinder = nextVersionTxtFileFinder;
            this.lastTaggedReleaseFinder = lastTaggedReleaseFinder;
            mergedBranchesWithVersionFinder = new MergedBranchesWithVersionFinder(context);
            unknownBranchFinder = new OtherBranchVersionFinder();
            this.context = context;
        }

        public SemanticVersion NextVersion()
        {
            return GetPossibleVersions().Max();
        }

        public IEnumerable<SemanticVersion> GetPossibleVersions()
        {
            // always provide a minimum fallback version for other strategies
            var defaultNextVersion = new SemanticVersion
            {
                Minor = 1
            };
            yield return defaultNextVersion;

            VersionTaggedCommit lastTaggedRelease;
            if (lastTaggedReleaseFinder.GetVersion(out lastTaggedRelease))
            {
                //If the exact commit is tagged then just return that commit
                if (context.CurrentCommit.Sha == lastTaggedRelease.Commit.Sha)
                {
                    yield return lastTaggedRelease.SemVer;
                    yield break;
                }
                defaultNextVersion = new SemanticVersion
                {
                    Major = lastTaggedRelease.SemVer.Major,
                    Minor = lastTaggedRelease.SemVer.Minor,
                    Patch = lastTaggedRelease.SemVer.Patch + 1
                };
                yield return defaultNextVersion;
            }

            SemanticVersion fileVersion;
            var hasNextVersionTxtVersion = nextVersionTxtFileFinder.TryGetNextVersion(out fileVersion);
            if (hasNextVersionTxtVersion && !string.IsNullOrEmpty(context.Configuration.NextVersion))
            {
                throw new Exception("You cannot specify a next version in both NextVersion.txt and GitVersionConfig.yaml. Please delete NextVersion.txt and use GitVersionConfig.yaml");
            }

            if (!string.IsNullOrEmpty(context.Configuration.NextVersion))
            {
                yield return SemanticVersion.Parse(context.Configuration.NextVersion, context.Configuration.TagPrefix);
            }

            if (hasNextVersionTxtVersion)
            {
                yield return fileVersion;
            }

            SemanticVersion tryGetVersion;
            if (mergedBranchesWithVersionFinder.TryGetVersion(out tryGetVersion))
            {
                yield return tryGetVersion;
            }

            SemanticVersion otherBranchVersion;
            if (unknownBranchFinder.FindVersion(context, defaultNextVersion, out otherBranchVersion))
            {
                yield return otherBranchVersion;
            }

        }
    }
}