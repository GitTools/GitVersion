using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

public class NextVersionCalculator : INextVersionCalculator
{
    private readonly ILog log;
    private readonly IMainlineVersionCalculator mainlineVersionCalculator;
    private readonly IRepositoryStore repositoryStore;
    private readonly Lazy<GitVersionContext> versionContext;
    private readonly IVersionStrategy[] versionStrategies;
    private readonly IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder;
    private readonly IIncrementStrategyFinder incrementStrategyFinder;

    private GitVersionContext Context => this.versionContext.Value;

    public NextVersionCalculator(ILog log,
                                 IMainlineVersionCalculator mainlineVersionCalculator,
                                 IRepositoryStore repositoryStore,
                                 Lazy<GitVersionContext> versionContext,
                                 IEnumerable<IVersionStrategy> versionStrategies,
                                 IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder,
                                 IIncrementStrategyFinder incrementStrategyFinder)
    {
        this.log = log.NotNull();
        this.mainlineVersionCalculator = mainlineVersionCalculator.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.incrementStrategyFinder = incrementStrategyFinder.NotNull();
        this.versionContext = versionContext.NotNull();
        this.versionStrategies = versionStrategies.NotNull().ToArray();
        this.effectiveBranchConfigurationFinder = effectiveBranchConfigurationFinder.NotNull();
        this.incrementStrategyFinder = incrementStrategyFinder.NotNull();
    }

    public virtual NextVersion FindVersion()
    {
        this.log.Info($"Running against branch: {Context.CurrentBranch} ({Context.CurrentCommit?.ToString() ?? "-"})");
        if (Context.IsCurrentCommitTagged)
        {
            this.log.Info($"Current commit is tagged with version {Context.CurrentCommitTaggedVersion}, " + "version calculation is for metadata only.");
        }
        else
        {
            EnsureHeadIsNotDetached(Context);
        }

        // TODO: It is totally unimportant that the current commit has been tagged or not IMO. We can make a double check actually if the result
        // is the same or make it configurable but each run should be deterministic.Even if the development process goes on the tagged commit
        // should always calculating the same result. Otherwise something is wrong with the configuration or someone messed up the branching history.

        SemanticVersion? taggedSemanticVersion = null;

        if (Context.IsCurrentCommitTagged)
        {
            // Will always be 0, don't bother with the +0 on tags
            var semanticVersionBuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(Context.CurrentCommit!);
            semanticVersionBuildMetaData.CommitsSinceTag = null;
            var semanticVersion = new SemanticVersion(Context.CurrentCommitTaggedVersion) { BuildMetaData = semanticVersionBuildMetaData };
            taggedSemanticVersion = semanticVersion;
        }

        //

        var nextVersion = Calculate(Context.CurrentBranch, Context.Configuration);
        nextVersion.BaseVersion.SemanticVersion.BuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource);
        SemanticVersion semver;
        if (Context.Configuration.VersioningMode == VersioningMode.Mainline)
        {
            semver = this.mainlineVersionCalculator.FindMainlineModeVersion(nextVersion.BaseVersion);
        }
        else
        {
            if (taggedSemanticVersion?.BuildMetaData == null || (taggedSemanticVersion.BuildMetaData?.Sha != nextVersion.BaseVersion.SemanticVersion.BuildMetaData.Sha))
            {
                semver = nextVersion.IncrementedVersion;
                semver.BuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(nextVersion.BaseVersion.BaseVersionSource);
            }
            else
            {
                semver = nextVersion.BaseVersion.SemanticVersion;
            }
        }

        var hasPreReleaseTag = semver.HasPreReleaseTagWithLabel;
        var tag = nextVersion.Configuration.Tag;
        var branchConfigHasPreReleaseTagConfigured = !tag.IsNullOrEmpty();
        var preReleaseTagDoesNotMatchConfiguration = hasPreReleaseTag && branchConfigHasPreReleaseTagConfigured && semver.PreReleaseTag?.Name != tag;
        if (semver.PreReleaseTag?.HasTag() != true && branchConfigHasPreReleaseTagConfigured || preReleaseTagDoesNotMatchConfiguration)
        {
            UpdatePreReleaseTag(new(nextVersion.Branch, nextVersion.Configuration), semver, nextVersion.BaseVersion.BranchNameOverride);
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

        return new(taggedSemanticVersion ?? semver, nextVersion.BaseVersion, new(nextVersion.Branch, nextVersion.Configuration));
    }

    private void UpdatePreReleaseTag(EffectiveBranchConfiguration configuration, SemanticVersion semanticVersion, string? branchNameOverride)
    {
        var tagToUse = configuration.Value.GetBranchSpecificTag(this.log, Context.CurrentBranch.Name.Friendly, branchNameOverride);

        long? number = null;

        // TODO: Please update the pre release-tag in the IVersionStrategy implementation.
        var lastTag = this.repositoryStore
            .GetVersionTagsOnBranch(Context.CurrentBranch, Context.Configuration.TagPrefix)
            .FirstOrDefault(v => v.PreReleaseTag?.Name?.IsEquivalentTo(tagToUse) == true);

        if (lastTag != null && MajorMinorPatchEqual(lastTag, semanticVersion) && lastTag.HasPreReleaseTagWithLabel)
        {
            number = lastTag.PreReleaseTag!.Number + 1;
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

    private NextVersion Calculate(IBranch branch, GitVersionConfiguration configuration)
    {
        using (log.IndentLog("Calculating the base versions"))
        {
            var nextVersions = GetNextVersions(branch, configuration).ToArray();
            var maxVersion = nextVersions.Max();

            var matchingVersionsOnceIncremented = nextVersions
                .Where(v => v.BaseVersion.BaseVersionSource != null && v.IncrementedVersion == maxVersion.IncrementedVersion)
                .ToList();
            ICommit? latestBaseVersionSource;

            if (matchingVersionsOnceIncremented.Any())
            {
                static NextVersion CompareVersions(
                    NextVersion versions1,
                    NextVersion version2)
                {
                    if (versions1.BaseVersion.BaseVersionSource == null)
                    {
                        return version2;
                    }
                    if (version2.BaseVersion.BaseVersionSource == null)
                    {
                        return versions1;
                    }

                    return versions1.BaseVersion.BaseVersionSource.When
                        < version2.BaseVersion.BaseVersionSource.When ? versions1 : version2;
                }

                var latestVersion = matchingVersionsOnceIncremented.Aggregate(CompareVersions);
                latestBaseVersionSource = latestVersion.BaseVersion.BaseVersionSource;
                maxVersion = latestVersion;
                log.Info($"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion})," +
                    $" taking oldest source for commit counting ({latestVersion.BaseVersion.Source})");
            }
            else
            {
                IEnumerable<NextVersion> filteredVersions = nextVersions;
                if (!maxVersion.IncrementedVersion.HasPreReleaseTagWithLabel)
                {
                    // If the maximal version has no pre-release tag defined than we want to determine just the latest previous
                    // base source which are not coming from pre-release tag.
                    filteredVersions = filteredVersions.Where(v => !v.BaseVersion.SemanticVersion.HasPreReleaseTagWithLabel);
                }

                var version = filteredVersions
                    .Where(v => v.BaseVersion.BaseVersionSource != null)
                    .OrderByDescending(v => v.IncrementedVersion)
                    .ThenByDescending(v => v.BaseVersion.BaseVersionSource!.When)
                    .FirstOrDefault();

                version ??= filteredVersions.Where(v => v.BaseVersion.BaseVersionSource == null)
                    .OrderByDescending(v => v.IncrementedVersion)
                    .First();
                latestBaseVersionSource = version.BaseVersion.BaseVersionSource;
            }

            var calculatedBase = new BaseVersion(
                maxVersion.BaseVersion.Source,
                maxVersion.BaseVersion.ShouldIncrement,
                maxVersion.BaseVersion.SemanticVersion,
                latestBaseVersionSource,
                maxVersion.BaseVersion.BranchNameOverride
            );

            log.Info($"Base version used: {calculatedBase}");

            var nextVersion = new NextVersion(maxVersion.IncrementedVersion, calculatedBase, maxVersion.Branch, maxVersion.Configuration);

            return nextVersion;
        }
    }

    private IEnumerable<NextVersion> GetNextVersions(IBranch branch, GitVersionConfiguration configuration)
    {
        if (branch.Tip == null)
            throw new GitVersionException("No commits found on the current branch.");

        bool atLeastOneBaseVersionReturned = false;

        foreach (var effectiveBranchConfiguration in effectiveBranchConfigurationFinder.GetConfigurations(branch, configuration))
        {
            foreach (var versionStrategy in this.versionStrategies)
            {
                foreach (var baseVersion in versionStrategy.GetBaseVersions(effectiveBranchConfiguration))
                {
                    log.Info(baseVersion.ToString());
                    if (IncludeVersion(baseVersion, configuration.Ignore))
                    {
                        var incrementStrategy = incrementStrategyFinder.DetermineIncrementedField(
                            context: Context,
                            baseVersion: baseVersion,
                            configuration: effectiveBranchConfiguration.Value
                        );
                        var incrementedVersion = incrementStrategy == VersionField.None
                            ? baseVersion.SemanticVersion
                            : baseVersion.SemanticVersion.IncrementVersion(incrementStrategy);

                        if (configuration.VersioningMode == VersioningMode.Mainline)
                        {
                            if (!(incrementedVersion.PreReleaseTag?.HasTag() != true))
                            {
                                continue;
                            }
                        }

                        yield return effectiveBranchConfiguration.CreateNextVersion(baseVersion, incrementedVersion);
                        atLeastOneBaseVersionReturned = true;
                    }
                }
            }
        }

        if (!atLeastOneBaseVersionReturned)
        {
            throw new GitVersionException("No base versions determined on the current branch.");
        }
    }

    private bool IncludeVersion(BaseVersion baseVersion, IgnoreConfiguration ignoreConfiguration)
    {
        foreach (var versionFilter in ignoreConfiguration.ToFilters())
        {
            if (versionFilter.Exclude(baseVersion, out var reason))
            {
                if (reason != null)
                {
                    log.Info(reason);
                }
                return false;
            }
        }
        return true;
    }
}
