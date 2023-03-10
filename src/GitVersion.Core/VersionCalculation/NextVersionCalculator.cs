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

        var nextVersion = Calculate(Context.CurrentBranch, Context.Configuration);
        var baseVersion = nextVersion.BaseVersion;
        var preReleaseTagName = nextVersion.Configuration.GetBranchSpecificTag(
            this.log, Context.CurrentBranch.Name.WithoutOrigin, baseVersion.BranchNameOverride
        );

        SemanticVersion semver;
        if (nextVersion.Configuration.VersioningMode == VersioningMode.Mainline)
        {
            semver = this.mainlineVersionCalculator.FindMainlineModeVersion(baseVersion);
        }
        else
        {
            var baseVersionBuildMetaData = this.mainlineVersionCalculator.CreateVersionBuildMetaData(baseVersion.BaseVersionSource);

            if (baseVersionBuildMetaData.Sha != nextVersion.IncrementedVersion.BuildMetaData.Sha)
            {
                semver = nextVersion.IncrementedVersion;
            }
            else
            {
                semver = baseVersion.SemanticVersion;
            }

            semver.BuildMetaData = baseVersionBuildMetaData;

            var lastPrefixedSemver = this.repositoryStore
                .GetVersionTagsOnBranch(Context.CurrentBranch, Context.Configuration.LabelPrefix, Context.Configuration.SemanticVersionFormat)
                .Where(v => MajorMinorPatchEqual(v, semver) && v.PreReleaseTag.HasTag())
                .FirstOrDefault(v => v.PreReleaseTag.Name?.IsEquivalentTo(preReleaseTagName) == true);

            if (lastPrefixedSemver != null)
            {
                semver.PreReleaseTag = lastPrefixedSemver.PreReleaseTag;
            }
        }

        if (semver.CompareTo(Context.CurrentCommitTaggedVersion) == 0)
        {
            // Will always be 0, don't bother with the +0 on tags
            semver.BuildMetaData.CommitsSinceTag = null;
        }
        else
        {
            long? number;

            if (semver.PreReleaseTag.Name == preReleaseTagName)
            {
                number = semver.PreReleaseTag.Number + 1;
            }
            else
            {
                number = 1;
            }

            semver.PreReleaseTag = new SemanticVersionPreReleaseTag(preReleaseTagName, number);
        }

        if (string.IsNullOrEmpty(preReleaseTagName))
        {
            semver.PreReleaseTag = new SemanticVersionPreReleaseTag();
        }

        return new(semver, baseVersion, new(nextVersion.Branch, nextVersion.Configuration));
    }

    private static bool MajorMinorPatchEqual(SemanticVersion version, SemanticVersion other) => version.CompareTo(other, false) == 0;

    private NextVersion Calculate(IBranch branch, GitVersionConfiguration configuration)
    {
        using (log.IndentLog("Calculating the base versions"))
        {
            var nextVersions = GetNextVersions(branch, configuration).ToArray();
            var maxVersion = nextVersions.Max();

            maxVersion.NotNull();

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

                var versions = filteredVersions as NextVersion[] ?? filteredVersions.ToArray();
                var version = versions
                    .Where(v => v.BaseVersion.BaseVersionSource != null)
                    .OrderByDescending(v => v.IncrementedVersion)
                    .ThenByDescending(v => v.BaseVersion.BaseVersionSource?.When)
                    .FirstOrDefault();

                version ??= versions.Where(v => v.BaseVersion.BaseVersionSource == null)
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

        return GetNextVersions();

        IEnumerable<NextVersion> GetNextVersions()
        {
            var atLeastOneBaseVersionReturned = false;

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

                            if (effectiveBranchConfiguration.Value.VersioningMode == VersioningMode.Mainline)
                            {
                                if (incrementedVersion.PreReleaseTag.HasTag())
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
