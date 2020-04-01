using System;
using System.Linq;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation
{
    public class NextVersionCalculator : INextVersionCalculator
    {
        private readonly ILog log;
        private readonly IBaseVersionCalculator baseVersionCalculator;
        private readonly IMainlineVersionCalculator mainlineVersionCalculator;
        private readonly IRepositoryMetadataProvider repositoryMetadataProvider;
        private readonly Lazy<GitVersionContext> versionContext;
        private GitVersionContext context => versionContext.Value;

        public NextVersionCalculator(ILog log, IBaseVersionCalculator baseVersionCalculator,
            IMainlineVersionCalculator mainlineVersionCalculator, IRepositoryMetadataProvider repositoryMetadataProvider,
            Lazy<GitVersionContext> versionContext)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.baseVersionCalculator = baseVersionCalculator ?? throw new ArgumentNullException(nameof(baseVersionCalculator));
            this.mainlineVersionCalculator = mainlineVersionCalculator ?? throw new ArgumentNullException(nameof(mainlineVersionCalculator));
            this.repositoryMetadataProvider = repositoryMetadataProvider ?? throw new ArgumentNullException(nameof(repositoryMetadataProvider));
            this.versionContext = versionContext ?? throw new ArgumentNullException(nameof(versionContext));
        }

        public SemanticVersion FindVersion()
        {
            log.Info($"Running against branch: {context.CurrentBranch.FriendlyName} ({(context.CurrentCommit == null ? "-" : context.CurrentCommit.Sha)})");
            if (context.IsCurrentCommitTagged)
            {
                log.Info($"Current commit is tagged with version {context.CurrentCommitTaggedVersion}, " +
                         "version calculation is for metadata only.");
            }
            EnsureHeadIsNotDetached(context);

            SemanticVersion taggedSemanticVersion = null;

            if (context.IsCurrentCommitTagged)
            {
                // Will always be 0, don't bother with the +0 on tags
                var semanticVersionBuildMetaData = mainlineVersionCalculator.CreateVersionBuildMetaData(context.CurrentCommit);
                semanticVersionBuildMetaData.CommitsSinceTag = null;

                var semanticVersion = new SemanticVersion(context.CurrentCommitTaggedVersion)
                {
                    BuildMetaData = semanticVersionBuildMetaData
                };
                taggedSemanticVersion = semanticVersion;
            }

            var baseVersion = baseVersionCalculator.GetBaseVersion();
            SemanticVersion semver;
            if (context.Configuration.VersioningMode == VersioningMode.Mainline)
            {
                semver = mainlineVersionCalculator.FindMainlineModeVersion(baseVersion);
            }
            else
            {
                semver = PerformIncrement(baseVersion);
                semver.BuildMetaData = mainlineVersionCalculator.CreateVersionBuildMetaData(baseVersion.BaseVersionSource);
            }

            var hasPreReleaseTag = semver.PreReleaseTag.HasTag();
            var branchConfigHasPreReleaseTagConfigured = !string.IsNullOrEmpty(context.Configuration.Tag);
            var preReleaseTagDoesNotMatchConfiguration = hasPreReleaseTag && branchConfigHasPreReleaseTagConfigured && semver.PreReleaseTag.Name != context.Configuration.Tag;
            if (!semver.PreReleaseTag.HasTag() && branchConfigHasPreReleaseTagConfigured || preReleaseTagDoesNotMatchConfiguration)
            {
                UpdatePreReleaseTag(semver, baseVersion.BranchNameOverride);
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

        private SemanticVersion PerformIncrement(BaseVersion baseVersion)
        {
            var semver = baseVersion.SemanticVersion;
            var increment = repositoryMetadataProvider.DetermineIncrementedField(baseVersion, context);
            if (increment != null)
            {
                semver = semver.IncrementVersion(increment.Value);
            }
            else log.Info("Skipping version increment");
            return semver;
        }

        private void UpdatePreReleaseTag(SemanticVersion semanticVersion, string branchNameOverride)
        {
            var tagToUse = context.Configuration.GetBranchSpecificTag(log, context.CurrentBranch.FriendlyName, branchNameOverride);

            int? number = null;

            var lastTag = repositoryMetadataProvider
                .GetVersionTagsOnBranch(context.CurrentBranch, context.Configuration.GitTagPrefix)
                .FirstOrDefault(v => v.PreReleaseTag.Name == tagToUse);

            if (lastTag != null &&
                MajorMinorPatchEqual(lastTag, semanticVersion) &&
                lastTag.PreReleaseTag.HasTag())
            {
                number = lastTag.PreReleaseTag.Number + 1;
            }

            number ??= 1;

            semanticVersion.PreReleaseTag = new SemanticVersionPreReleaseTag(tagToUse, number);
        }

        private static void EnsureHeadIsNotDetached(GitVersionContext context)
        {
            if (!context.CurrentBranch.IsDetachedHead())
            {
                return;
            }

            var message = string.Format(
                "It looks like the branch being examined is a detached Head pointing to commit '{0}'. " +
                "Without a proper branch name GitVersion cannot determine the build version.",
                context.CurrentCommit.Id.ToString(7));
            throw new WarningException(message);
        }

        private static bool MajorMinorPatchEqual(SemanticVersion lastTag, SemanticVersion baseVersion)
        {
            return lastTag.Major == baseVersion.Major &&
                   lastTag.Minor == baseVersion.Minor &&
                   lastTag.Patch == baseVersion.Patch;
        }
    }
}
