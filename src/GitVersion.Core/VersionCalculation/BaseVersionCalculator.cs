using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

public class BaseVersionCalculator : IBaseVersionCalculator
{
    private readonly ILog log;
    private readonly IRepositoryStore repositoryStore;
    private readonly IVersionStrategy[] strategies;
    private readonly Lazy<GitVersionContext> versionContext;
    private GitVersionContext context => this.versionContext.Value;
    private readonly IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder;
    private readonly IIncrementStrategyFinder incrementStrategyFinder;

    public BaseVersionCalculator(ILog log, IRepositoryStore repositoryStore,
        Lazy<GitVersionContext> versionContext, IEnumerable<IVersionStrategy> strategies,
        IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder, IIncrementStrategyFinder incrementStrategyFinder)
    {
        this.log = log.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.strategies = strategies.ToArray();
        this.versionContext = versionContext.NotNull();
        this.effectiveBranchConfigurationFinder = effectiveBranchConfigurationFinder.NotNull();
        this.incrementStrategyFinder = incrementStrategyFinder.NotNull();
    }

    public NextVersion Calculate(IBranch branch, Config configuration)
    {
        using (log.IndentLog("Calculating the base versions"))
        {
            var nextVersions = GetPotentialNextVersions(branch, configuration).ToArray();

            FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(nextVersions);

            var maxVersion = nextVersions.Aggregate((v1, v2) => v1.IncrementedVersion > v2.IncrementedVersion ? v1 : v2);
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
                if (!maxVersion.IncrementedVersion.PreReleaseTag!.HasTag())
                {
                    // If the maximal version has no pre-release tag defined than we want to determine just the latest previous
                    // base source which are not comming from pre-release tag.
                    filteredVersions = filteredVersions.Where(v => !v.BaseVersion.SemanticVersion.PreReleaseTag!.HasTag());
                }

                var version = filteredVersions
                    .Where(v => v.BaseVersion.BaseVersionSource != null)
                    .OrderByDescending(v => v.IncrementedVersion)
                    .ThenByDescending(v => v.BaseVersion.BaseVersionSource!.When)
                    .FirstOrDefault();

                if (version == null)
                {
                    version = filteredVersions.Where(v => v.BaseVersion.BaseVersionSource == null)
                        .OrderByDescending(v => v.IncrementedVersion)
                        .First();
                }
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

            return new(maxVersion.IncrementedVersion, calculatedBase, maxVersion.Branch, maxVersion.Configuration);
        }
    }

    private IEnumerable<NextVersion> GetPotentialNextVersions(IBranch branch, Config configuration)
    {
        if (branch.Tip == null)
            throw new GitVersionException("No commits found on the current branch.");

        bool atLeastOneBaseVersionReturned = false;

        foreach (var effectiveBranchConfiguration in effectiveBranchConfigurationFinder.GetConfigurations(branch, configuration))
        {
            // Has been moved from BaseVersionCalculator because the effected configuration is only available in this class.
            context.Configuration = effectiveBranchConfiguration.Configuration;

            foreach (var strategy in strategies)
            {
                foreach (var baseVersion in strategy.GetBaseVersions(effectiveBranchConfiguration))
                {
                    log.Info(baseVersion.ToString());
                    if (IncludeVersion(baseVersion, configuration.Ignore))
                    {
                        var incrementStrategy = incrementStrategyFinder.DetermineIncrementedField(
                            context: context,
                            baseVersion: baseVersion,
                            configuration: effectiveBranchConfiguration.Configuration
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

    private bool IncludeVersion(BaseVersion baseVersion, IgnoreConfig ignoreConfiguration)
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

    private void FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(IEnumerable<NextVersion> nextVersions)
    {
        if (ReleaseBranchExistsInRepo()) return;

        foreach (var nextVersion in nextVersions)
        {
            if (nextVersion.BaseVersion.Source.Contains(MergeMessageVersionStrategy.MergeMessageStrategyPrefix)
                && nextVersion.BaseVersion.Source.Contains("Merge branch")
                && nextVersion.BaseVersion.Source.Contains("release"))
            {
                if (nextVersion.BaseVersion.BaseVersionSource != null)
                {
                    var parents = nextVersion.BaseVersion.BaseVersionSource.Parents.ToList();
                    nextVersion.BaseVersion = new BaseVersion(
                        nextVersion.BaseVersion.Source,
                        nextVersion.BaseVersion.ShouldIncrement,
                        nextVersion.BaseVersion.SemanticVersion,
                        this.repositoryStore.FindMergeBase(parents[0], parents[1]),
                        nextVersion.BaseVersion.BranchNameOverride);
                }
            }
        }
    }

    private bool ReleaseBranchExistsInRepo()
    {
        var releaseBranchConfig = context.FullConfiguration.GetReleaseBranchConfig();
        var releaseBranches = this.repositoryStore.GetReleaseBranches(releaseBranchConfig);
        return releaseBranches.Any();
    }
}
