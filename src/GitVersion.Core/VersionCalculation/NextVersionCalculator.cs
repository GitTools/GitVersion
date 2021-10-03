using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

public class NextVersionCalculator : INextVersionCalculator
{
    private readonly ILog log;
    private readonly IBaseVersionCalculator baseVersionCalculator;
    private readonly IMainlineVersionCalculator mainlineVersionCalculator;
    private readonly IRepositoryStore repositoryStore;
    private readonly Lazy<GitVersionContext> versionContext;
    private GitVersionContext context => this.versionContext.Value;

    public NextVersionCalculator(ILog log, IBaseVersionCalculator baseVersionCalculator,
        IMainlineVersionCalculator mainlineVersionCalculator, IRepositoryStore repositoryStore,
        Lazy<GitVersionContext> versionContext)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.baseVersionCalculator = baseVersionCalculator ?? throw new ArgumentNullException(nameof(baseVersionCalculator));
        this.mainlineVersionCalculator = mainlineVersionCalculator ?? throw new ArgumentNullException(nameof(mainlineVersionCalculator));
        this.repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));
        this.versionContext = versionContext ?? throw new ArgumentNullException(nameof(versionContext));
    }

    public SemanticVersion FindVersion()
    {
        this.log.Info($"Running against branch: {context.CurrentBranch} ({context.CurrentCommit?.ToString() ?? "-"})");
        if (context.IsCurrentCommitTagged)
        {
            this.log.Info($"Current commit is tagged with version {context.CurrentCommitTaggedVersion}, " +
                          "version calculation is for metadata only.");
        }
        else
        {
            EnsureHeadIsNotDetached(context);
        }

        SemanticVersion? taggedSemanticVersion = null;

        if (context.IsCurrentCommitTagged)
        {
            // Will always be 0, don't bother with the +0 on tags
            var semanticVersionBuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(context.CurrentCommit);
            semanticVersionBuildMetaData.CommitsSinceTag = null;

            var semanticVersion = new SemanticVersion(context.CurrentCommitTaggedVersion)
            {
                BuildMetaData = semanticVersionBuildMetaData
            };
            taggedSemanticVersion = semanticVersion;
        }

        var baseVersion = this.baseVersionCalculator.GetBaseVersion();
        baseVersion.SemanticVersion.BuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(baseVersion.BaseVersionSource);
        SemanticVersion semver;
        if (context.Configuration?.VersioningMode == VersioningMode.Mainline)
        {
            semver = this.mainlineVersionCalculator.FindMainlineModeVersion(baseVersion);
        }
        else
        {
            if (taggedSemanticVersion?.BuildMetaData == null || (taggedSemanticVersion.BuildMetaData.Sha != baseVersion.SemanticVersion.BuildMetaData.Sha))
            {
                semver = PerformIncrement(baseVersion);
                semver.BuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(baseVersion.BaseVersionSource);
            }
            else
            {
                semver = baseVersion.SemanticVersion;
            }
        }

        var hasPreReleaseTag = semver.PreReleaseTag?.HasTag() == true;
        var tag = context.Configuration?.Tag;
        var branchConfigHasPreReleaseTagConfigured = !tag.IsNullOrEmpty();
#pragma warning disable CS8602 // Dereference of a possibly null reference. // context.Configuration.Tag not null when branchConfigHasPreReleaseTagConfigured is true
        var preReleaseTagDoesNotMatchConfiguration = hasPreReleaseTag && branchConfigHasPreReleaseTagConfigured && semver.PreReleaseTag?.Name != tag;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        if (semver.PreReleaseTag?.HasTag() != true && branchConfigHasPreReleaseTagConfigured || preReleaseTagDoesNotMatchConfiguration)
        {
            UpdatePreReleaseTag(semver, baseVersion?.BranchNameOverride);
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

        return taggedSemanticVersion ?? semver;
    }

    private SemanticVersion PerformIncrement(BaseVersion baseVersion)
    {
        var semver = baseVersion.SemanticVersion;
        var increment = this.repositoryStore.DetermineIncrementedField(baseVersion, context);
        if (increment != null)
        {
            semver = semver.IncrementVersion(increment.Value);
        }
        else this.log.Info("Skipping version increment");
        return semver;
    }

    private void UpdatePreReleaseTag(SemanticVersion semanticVersion, string? branchNameOverride)
    {
        var tagToUse = context.Configuration?.GetBranchSpecificTag(this.log, context.CurrentBranch?.Name.Friendly, branchNameOverride);

        int? number = null;

        var lastTag = this.repositoryStore
            .GetVersionTagsOnBranch(context.CurrentBranch!, context.Configuration?.GitTagPrefix)
            .FirstOrDefault(v => v.PreReleaseTag?.Name?.IsEquivalentTo(tagToUse) == true);

        if (lastTag != null &&
            MajorMinorPatchEqual(lastTag, semanticVersion) &&
            lastTag.PreReleaseTag?.HasTag() == true)
        {
            number = lastTag.PreReleaseTag.Number + 1;
        }

        number ??= 1;

        semanticVersion.PreReleaseTag = new SemanticVersionPreReleaseTag(tagToUse, number);
    }

    private static void EnsureHeadIsNotDetached(GitVersionContext context)
    {
        if (context.CurrentBranch?.IsDetachedHead != true)
        {
            return;
        }

        var message = string.Format(
            "It looks like the branch being examined is a detached Head pointing to commit '{0}'. " +
            "Without a proper branch name GitVersion cannot determine the build version.",
            context.CurrentCommit?.Id.ToString(7));
        throw new WarningException(message);
    }

    private static bool MajorMinorPatchEqual(SemanticVersion lastTag, SemanticVersion baseVersion) => lastTag.Major == baseVersion.Major &&
                                                                                                      lastTag.Minor == baseVersion.Minor &&
                                                                                                      lastTag.Patch == baseVersion.Patch;
}
