using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public class NextVersionCalculator : INextVersionCalculator
{
    private readonly ILog log;
    private readonly IBaseVersionCalculator baseVersionCalculator;
    private readonly IMainlineVersionCalculator mainlineVersionCalculator;
    private readonly IRepositoryStore repositoryStore;
    private readonly IIncrementStrategyFinder incrementStrategyFinder;
    private readonly Lazy<GitVersionContext> versionContext;

    private GitVersionContext Context => this.versionContext.Value;

    public NextVersionCalculator(
        ILog log,
        IBaseVersionCalculator baseVersionCalculator,
        IMainlineVersionCalculator mainlineVersionCalculator,
        IRepositoryStore repositoryStore,
        IIncrementStrategyFinder incrementStrategyFinder,
        Lazy<GitVersionContext> versionContext)
    {
        this.log = log.NotNull();
        this.baseVersionCalculator = baseVersionCalculator.NotNull();
        this.mainlineVersionCalculator = mainlineVersionCalculator.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.incrementStrategyFinder = incrementStrategyFinder.NotNull();
        this.versionContext = versionContext.NotNull();
    }

    public NextVersion FindVersion()
    {
        this.log.Info($"Running against branch: {Context.CurrentBranch} ({Context.CurrentCommit?.ToString() ?? "-"})");
        if (Context.IsCurrentCommitTagged)
        {
            this.log.Info($"Current commit is tagged with version {Context.CurrentCommitTaggedVersion}, " + "version calculation is for metadata only.");
        }

        SemanticVersion? taggedSemanticVersion = null;

        if (Context.IsCurrentCommitTagged)
        {
            // Will always be 0, don't bother with the +0 on tags
            var semanticVersionBuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(Context.CurrentCommit);
            semanticVersionBuildMetaData.CommitsSinceTag = null;

            var semanticVersion = new SemanticVersion(Context.CurrentCommitTaggedVersion) { BuildMetaData = semanticVersionBuildMetaData };
            taggedSemanticVersion = semanticVersion;
        }

        var (baseVersion, configuration) = this.baseVersionCalculator.GetBaseVersion();
        baseVersion.SemanticVersion.BuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(baseVersion.BaseVersionSource);
        SemanticVersion semver;
        if (Context.FullConfiguration.VersioningMode == VersioningMode.Mainline)
        {
            semver = this.mainlineVersionCalculator.FindMainlineModeVersion(baseVersion);
        }
        else
        {
            if (taggedSemanticVersion?.BuildMetaData == null || (taggedSemanticVersion.BuildMetaData?.Sha != baseVersion.SemanticVersion.BuildMetaData.Sha))
            {
                semver = PerformIncrement(baseVersion, configuration.Value);
                semver.BuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(baseVersion.BaseVersionSource);
            }
            else
            {
                semver = baseVersion.SemanticVersion;
            }
        }

        var hasPreReleaseTag = semver.PreReleaseTag?.HasTag() == true;
        var tag = configuration.Value.Tag;
        var branchConfigHasPreReleaseTagConfigured = !tag.IsNullOrEmpty();
        var preReleaseTagDoesNotMatchConfiguration = hasPreReleaseTag && branchConfigHasPreReleaseTagConfigured && semver.PreReleaseTag?.Name != tag;
        if (semver.PreReleaseTag?.HasTag() != true && branchConfigHasPreReleaseTagConfigured || preReleaseTagDoesNotMatchConfiguration)
        {
            UpdatePreReleaseTag(configuration.Value, semver, baseVersion.BranchNameOverride);
        }

        if (taggedSemanticVersion != null)
        {
            // replace calculated version with tagged version only if tagged version greater or equal to calculated version
            if (semver.CompareTo(taggedSemanticVersion, false) > 0)
            {
                taggedSemanticVersion = null;
            }
            else if (taggedSemanticVersion.BuildMetaData != null)
            {
                // set the commit count on the tagged ver
                taggedSemanticVersion.BuildMetaData.CommitsSinceVersionSource = semver.BuildMetaData?.CommitsSinceVersionSource;
            }
        }

        var incrementedVersion = taggedSemanticVersion ?? semver;
        return new(incrementedVersion, baseVersion, configuration);
    }

    private SemanticVersion PerformIncrement(BaseVersion baseVersion, EffectiveConfiguration configuration)
    {
        var semver = baseVersion.SemanticVersion;
        var increment = this.incrementStrategyFinder.DetermineIncrementedField(Context, baseVersion, configuration);
        semver = semver.IncrementVersion(increment);
        return semver;
    }

    private void UpdatePreReleaseTag(EffectiveConfiguration configuration, SemanticVersion semanticVersion, string? branchNameOverride)
    {
        var tagToUse = configuration.GetBranchSpecificTag(this.log, Context.CurrentBranch.Name.Friendly, branchNameOverride);

        long? number = null;

        var lastTag = this.repositoryStore
            .GetVersionTagsOnBranch(Context.CurrentBranch, Context.FullConfiguration.TagPrefix)
            .FirstOrDefault(v => v.PreReleaseTag?.Name?.IsEquivalentTo(tagToUse) == true);

        if (lastTag != null && MajorMinorPatchEqual(lastTag, semanticVersion) && lastTag.PreReleaseTag?.HasTag() == true)
        {
            number = lastTag.PreReleaseTag.Number + 1;
        }

        number ??= 1;

        semanticVersion.PreReleaseTag = new SemanticVersionPreReleaseTag(tagToUse, number);
    }

    private static void EnsureHeadIsNotDetached(GitVersionContext context)
    {
        if (context.CurrentBranch.IsDetachedHead != true)
        {
            return;
        }

        var message = string.Format(
            "It looks like the branch being examined is a detached Head pointing to commit '{0}'. " + "Without a proper branch name GitVersion cannot determine the build version.",
            context.CurrentCommit?.Id.ToString(7));
        throw new WarningException(message);
    }

    private static bool MajorMinorPatchEqual(SemanticVersion lastTag, SemanticVersion baseVersion) => lastTag.Major == baseVersion.Major && lastTag.Minor == baseVersion.Minor && lastTag.Patch == baseVersion.Patch;
}
