using System;
using System.Linq;
using System.Text.RegularExpressions;

using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersion.VersioningModes;
using GitVersion.Configuration;
using GitVersion.Logging;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation
{
    public class NextVersionCalculator : INextVersionCalculator
    {
        private readonly ILog log;
        private readonly IBaseVersionCalculator baseVersionCalculator;
        private readonly IMainlineVersionCalculator mainlineVersionCalculator;
        private readonly IMetaDataCalculator metaDataCalculator;

        public NextVersionCalculator(ILog log, IMetaDataCalculator metaDataCalculator, IBaseVersionCalculator baseVersionCalculator, IMainlineVersionCalculator mainlineVersionCalculator)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.metaDataCalculator = metaDataCalculator ?? throw new ArgumentNullException(nameof(metaDataCalculator));

            this.baseVersionCalculator = baseVersionCalculator ?? throw new ArgumentNullException(nameof(baseVersionCalculator));
            this.mainlineVersionCalculator = mainlineVersionCalculator ?? throw new ArgumentNullException(nameof(mainlineVersionCalculator));
        }

        public SemanticVersion FindVersion(GitVersionContext context)
        {
            SemanticVersion taggedSemanticVersion = null;

            if (context.IsCurrentCommitTagged)
            {
                // Will always be 0, don't bother with the +0 on tags
                var semanticVersionBuildMetaData = metaDataCalculator.Create(context.CurrentCommit, context);
                semanticVersionBuildMetaData.CommitsSinceTag = null;

                var semanticVersion = new SemanticVersion(context.CurrentCommitTaggedVersion)
                {
                    BuildMetaData = semanticVersionBuildMetaData
                };
                taggedSemanticVersion = semanticVersion;
            }

            var baseVersion = baseVersionCalculator.GetBaseVersion(context);
            SemanticVersion semver;
            if (context.Configuration.VersioningMode == VersioningMode.Mainline)
            {
                semver = mainlineVersionCalculator.FindMainlineModeVersion(baseVersion, context);
            }
            else
            {
                semver = PerformIncrement(context, baseVersion);
                semver.BuildMetaData = metaDataCalculator.Create(baseVersion.BaseVersionSource, context);
            }

            var hasPreReleaseTag = semver.PreReleaseTag.HasTag();
            var branchConfigHasPreReleaseTagConfigured = !string.IsNullOrEmpty(context.Configuration.Tag);
            var preReleaseTagDoesNotMatchConfiguration = hasPreReleaseTag && branchConfigHasPreReleaseTagConfigured && semver.PreReleaseTag.Name != context.Configuration.Tag;
            if (!semver.PreReleaseTag.HasTag() && branchConfigHasPreReleaseTagConfigured || preReleaseTagDoesNotMatchConfiguration)
            {
                UpdatePreReleaseTag(context, semver, baseVersion.BranchNameOverride);
            }

            if (taggedSemanticVersion != null)
            {
                // replace calculated version with tagged version only if tagged version greater or equal to calculated version
                if (semver.CompareTo(taggedSemanticVersion, false) > 0)
                {
                    taggedSemanticVersion = null;
                }
                else
                {
                    // set the commit count on the tagged ver
                    taggedSemanticVersion.BuildMetaData.CommitsSinceVersionSource = semver.BuildMetaData.CommitsSinceVersionSource;
                }
            }

            return taggedSemanticVersion ?? semver;
        }

        private SemanticVersion PerformIncrement(GitVersionContext context, BaseVersion baseVersion)
        {
            var semver = baseVersion.SemanticVersion;
            var increment = IncrementStrategyFinder.DetermineIncrementedField(context, baseVersion);
            if (increment != null)
            {
                semver = semver.IncrementVersion(increment.Value);
            }
            else log.Info("Skipping version increment");
            return semver;
        }

        private void UpdatePreReleaseTag(GitVersionContext context, SemanticVersion semanticVersion, string branchNameOverride)
        {
            var tagToUse = GetBranchSpecificTag(context.Configuration, context.CurrentBranch.FriendlyName, branchNameOverride);

            int? number = null;

            var lastTag = context.RepositoryMetadataProvider
                .GetVersionTagsOnBranch(context.CurrentBranch, context.Configuration.GitTagPrefix)
                .FirstOrDefault(v => v.PreReleaseTag.Name == tagToUse);

            if (lastTag != null &&
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

        public string GetBranchSpecificTag(EffectiveConfiguration configuration, string branchFriendlyName, string branchNameOverride)
        {
            var tagToUse = configuration.Tag;
            if (tagToUse == "useBranchName")
            {
                tagToUse = "{BranchName}";
            }
            if (tagToUse.Contains("{BranchName}"))
            {
                log.Info("Using branch name to calculate version tag");

                var branchName = branchNameOverride ?? branchFriendlyName;
                if (!string.IsNullOrWhiteSpace(configuration.BranchPrefixToTrim))
                {
                    branchName = branchName.RegexReplace(configuration.BranchPrefixToTrim, string.Empty, RegexOptions.IgnoreCase);
                }
                branchName = branchName.RegexReplace("[^a-zA-Z0-9-]", "-");

                tagToUse = tagToUse.Replace("{BranchName}", branchName);
            }
            return tagToUse;
        }

        private static bool MajorMinorPatchEqual(SemanticVersion lastTag, SemanticVersion baseVersion)
        {
            return lastTag.Major == baseVersion.Major &&
                   lastTag.Minor == baseVersion.Minor &&
                   lastTag.Patch == baseVersion.Patch;
        }
    }
}
