using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersion.VersioningModes;
using GitVersion.Configuration;
using GitVersion.Helpers;
using GitVersion.Log;

namespace GitVersion.VersionCalculation
{
    public class NextVersionCalculator
    {
        private readonly ILog log;
        IBaseVersionCalculator baseVersionFinder;
        IMetaDataCalculator metaDataCalculator;

        public NextVersionCalculator(ILog log, IBaseVersionCalculator baseVersionCalculator = null, IMetaDataCalculator metaDataCalculator = null)
        {
            this.log = log;
            this.metaDataCalculator = metaDataCalculator ?? new MetaDataCalculator();
            baseVersionFinder = baseVersionCalculator ??
                new BaseVersionCalculator(log, 
                    new FallbackBaseVersionStrategy(),
                    new ConfigNextVersionBaseVersionStrategy(),
                    new TaggedCommitVersionStrategy(),
                    new MergeMessageBaseVersionStrategy(),
                    new VersionInBranchNameBaseVersionStrategy(),
                    new TrackReleaseBranchesVersionStrategy());
        }

        public SemanticVersion FindVersion(GitVersionContext context)
        {
            SemanticVersion taggedSemanticVersion = null;
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
                taggedSemanticVersion = semanticVersion;
            }

            var baseVersion = baseVersionFinder.GetBaseVersion(context);
            SemanticVersion semver;
            if (context.Configuration.VersioningMode == VersioningMode.Mainline)
            {
                var mainlineMode = new MainlineVersionCalculator(metaDataCalculator, log);
                semver = mainlineMode.FindMainlineModeVersion(baseVersion, context);
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
                // set the commit count on the tagged ver
                taggedSemanticVersion.BuildMetaData.CommitsSinceVersionSource = semver.BuildMetaData.CommitsSinceVersionSource;
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

        void UpdatePreReleaseTag(GitVersionContext context, SemanticVersion semanticVersion, string branchNameOverride)
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

        static bool MajorMinorPatchEqual(SemanticVersion lastTag, SemanticVersion baseVersion)
        {
            return lastTag.Major == baseVersion.Major &&
                   lastTag.Minor == baseVersion.Minor &&
                   lastTag.Patch == baseVersion.Patch;
        }
    }
}
