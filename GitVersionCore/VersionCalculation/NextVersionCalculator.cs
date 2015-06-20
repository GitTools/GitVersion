namespace GitVersion.VersionCalculation
{
    using System.Text.RegularExpressions;
    using GitVersion.VersionCalculation.BaseVersionCalculators;

    public class NextVersionCalculator
    {
        IBaseVersionCalculator baseVersionFinder;
        IMetaDataCalculator metaDataCalculator;
        HighestTagBaseVersionStrategy highestTagBaseVersionStrategy;

        public NextVersionCalculator(IBaseVersionCalculator baseVersionCalculator = null, IMetaDataCalculator metaDataCalculator = null)
        {
            this.metaDataCalculator = metaDataCalculator ?? new MetaDataCalculator();
            highestTagBaseVersionStrategy = new HighestTagBaseVersionStrategy();
            baseVersionFinder = baseVersionCalculator ??
                new BaseVersionCalculator(
                    new FallbackBaseVersionStrategy(),
                    new ConfigNextVersionBaseVersionStrategy(),
                    highestTagBaseVersionStrategy,
                    new TrackMergeTargetBaseVersionStrategy(),
                    new MergeMessageBaseVersionStrategy(),
                    new VersionInBranchBaseVersionStrategy());
        }

        public SemanticVersion FindVersion(GitVersionContext context)
        {
            // If current commit is tagged, don't do anything except add build metadata
            if (context.IsCurrentCommitTagged)
            {
                // Will always be 0, don't bother with the +0 on tags
                var semanticVersionBuildMetaData = metaDataCalculator.Create(context.CurrentCommit, context);
                semanticVersionBuildMetaData.CommitsSinceTag = null;
                var semanticVersion = new SemanticVersion(context.CurrentCommitTaggedVersion)
                {
                    BuildMetaData = semanticVersionBuildMetaData
                };
                return semanticVersion;
            }

            var baseVersion = baseVersionFinder.GetBaseVersion(context);
            var semver = baseVersion.SemanticVersion;
            if (baseVersion.ShouldIncrement)
            {
                semver = semver.IncrementVersion(context.Configuration.Increment);
            }
            else Logger.WriteInfo("Skipping version increment");

            if (!semver.PreReleaseTag.HasTag() && !string.IsNullOrEmpty(context.Configuration.Tag))
            {
                UpdatePreReleaseTag(context, semver, baseVersion.BranchNameOverride);
            }

            semver.BuildMetaData = metaDataCalculator.Create(baseVersion.BaseVersionSource, context);

            return semver;
        }

        void UpdatePreReleaseTag(GitVersionContext context, SemanticVersion semanticVersion, string branchNameOverride)
        {
            var tagToUse = context.Configuration.Tag;
            if (tagToUse == "useBranchName")
            {
                Logger.WriteInfo("Using branch name to calculate version tag");
                var name = branchNameOverride ?? context.CurrentBranch.Name;
                tagToUse = name.RegexReplace(context.Configuration.BranchPrefixToTrim, string.Empty, RegexOptions.IgnoreCase);
            }
            int? number = null;
            if (!string.IsNullOrEmpty(context.Configuration.TagNumberPattern))
            {
                var match = Regex.Match(context.CurrentBranch.CanonicalName, context.Configuration.TagNumberPattern);
                var numberGroup = match.Groups["number"];
                if (numberGroup.Success)
                {
                    number = int.Parse(numberGroup.Value);
                }
            }

            var lastTag = highestTagBaseVersionStrategy.GetVersion(context);
            if (number == null &&
                lastTag != null &&
                MajorMinorPatchEqual(lastTag.SemanticVersion, semanticVersion) &&
                lastTag.SemanticVersion.PreReleaseTag.HasTag())
            {
                number = lastTag.SemanticVersion.PreReleaseTag.Number + 1;
            }

            if (number == null)
            {
                number = 1;
            }

            semanticVersion.PreReleaseTag = new SemanticVersionPreReleaseTag(tagToUse, number);
        }

        static bool MajorMinorPatchEqual(SemanticVersion lastTag, SemanticVersion baseVersion)
        {
            return lastTag.Major == baseVersion.Major &&
                   lastTag.Minor == baseVersion.Minor &&
                   lastTag.Patch == baseVersion.Patch;
        }
    }
}