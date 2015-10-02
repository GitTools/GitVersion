﻿namespace GitVersion.VersionCalculation
{
    using System.Text.RegularExpressions;
    using BaseVersionCalculators;

    public class NextVersionCalculator
    {
        IBaseVersionCalculator baseVersionFinder;
        IMetaDataCalculator metaDataCalculator;

        public NextVersionCalculator(IBaseVersionCalculator baseVersionCalculator = null, IMetaDataCalculator metaDataCalculator = null)
        {
            this.metaDataCalculator = metaDataCalculator ?? new MetaDataCalculator();
            baseVersionFinder = baseVersionCalculator ??
                new BaseVersionCalculator(
                    new FallbackBaseVersionStrategy(),
                    new ConfigNextVersionBaseVersionStrategy(),
                    new TaggedCommitVersionStrategy(),
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
            var increment = IncrementStrategyFinder.DetermineIncrementedField(context, baseVersion);
            if (increment != null)
            {
                semver = semver.IncrementVersion(increment.Value);
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
            
            var lastTag = context.CurrentBranch.LastVersionTagOnBranch(context.Repository, context.Configuration.GitTagPrefix);
            if (number == null &&
                lastTag != null &&
                MajorMinorPatchEqual(lastTag, semanticVersion) &&
                lastTag.PreReleaseTag.HasTag())
            {
                number = lastTag.PreReleaseTag.Number + 1;
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