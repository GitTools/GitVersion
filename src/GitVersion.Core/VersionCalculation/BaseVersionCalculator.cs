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
                .Where(v => v.Version.BaseVersionSource != null && v.IncrementedVersion == maxVersion.IncrementedVersion)
                .ToList();
            ICommit? latestBaseVersionSource;

            if (matchingVersionsOnceIncremented.Any())
            {
                static NextVersion CompareVersions(
                    NextVersion versions1,
                    NextVersion version2)
                {
                    if (versions1.Version.BaseVersionSource == null)
                    {
                        return version2;
                    }
                    if (version2.Version.BaseVersionSource == null)
                    {
                        return versions1;
                    }

                    return versions1.Version.BaseVersionSource.When
                        < version2.Version.BaseVersionSource.When ? versions1 : version2;
                }

                var latestVersion = matchingVersionsOnceIncremented.Aggregate(CompareVersions);
                latestBaseVersionSource = latestVersion.Version.BaseVersionSource;
                maxVersion = latestVersion;
                log.Info($"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion})," +
                    $" taking oldest source for commit counting ({latestVersion.Version.Source})");
            }
            else
            {
                IEnumerable<NextVersion> filteredVersions = nextVersions;
                if (!maxVersion.IncrementedVersion.PreReleaseTag!.HasTag())
                {
                    // If the maximal version has no pre-release tag defined than we want to determine just the latest previous
                    // base source which are not comming from pre-release tag.
                    filteredVersions = filteredVersions.Where(v => !v.Version.SemanticVersion.PreReleaseTag!.HasTag());
                }

                var version = filteredVersions
                    .Where(v => v.Version.BaseVersionSource != null)
                    .OrderByDescending(v => v.IncrementedVersion)
                    .ThenByDescending(v => v.Version.BaseVersionSource!.When)
                    .FirstOrDefault();

                if (version == null)
                {
                    version = filteredVersions.Where(v => v.Version.BaseVersionSource == null)
                        .OrderByDescending(v => v.IncrementedVersion)
                        .First();
                }
                latestBaseVersionSource = version.Version.BaseVersionSource;
            }

            var calculatedBase = new BaseVersion(
                maxVersion.Version.Source,
                maxVersion.Version.ShouldIncrement,
                maxVersion.Version.SemanticVersion,
                latestBaseVersionSource,
                maxVersion.Version.BranchNameOverride
            );

            log.Info($"Base version used: {calculatedBase}");

            return new(maxVersion.IncrementedVersion, calculatedBase);
        }
    }

    private IEnumerable<NextVersion> GetPotentialNextVersions(IBranch branch, Config configuration)
    {
        if (branch.Tip == null)
            throw new GitVersionException("No commits found on the current branch.");

        bool atLeastOneBaseVersionReturned = false;

        foreach (var item in effectiveBranchConfigurationFinder.GetConfigurations(branch, configuration))
        {
            // Has been moved from BaseVersionCalculator because the effected configuration is only available in this class.
            context.Configuration = item.Configuration;

            foreach (var strategy in strategies)
            {
                foreach (var baseVersion in strategy.GetVersions(item.Branch, item.Configuration))
                {
                    log.Info(baseVersion.ToString());
                    if (IncludeVersion(baseVersion, configuration.Ignore))
                    {
                        var incrementStrategy = incrementStrategyFinder.DetermineIncrementedField(
                            context: context,
                            baseVersion: baseVersion,
                            configuration: item.Configuration
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
                        yield return new(incrementedVersion, baseVersion);
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
            if (nextVersion.Version.Source.Contains(MergeMessageVersionStrategy.MergeMessageStrategyPrefix)
                && nextVersion.Version.Source.Contains("Merge branch")
                && nextVersion.Version.Source.Contains("release"))
            {
                if (nextVersion.Version.BaseVersionSource != null)
                {
                    var parents = nextVersion.Version.BaseVersionSource.Parents.ToList();
                    nextVersion.Version = new BaseVersion(
                        nextVersion.Version.Source,
                        nextVersion.Version.ShouldIncrement,
                        nextVersion.Version.SemanticVersion,
                        this.repositoryStore.FindMergeBase(parents[0], parents[1]),
                        nextVersion.Version.BranchNameOverride);
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
