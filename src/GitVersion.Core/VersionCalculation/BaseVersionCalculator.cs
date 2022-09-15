using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

public class BaseVersionCalculator : IBaseVersionCalculator
{
    private readonly ILog log;
    private readonly IRepositoryStore repositoryStore;
    private readonly IVersionStrategy[] strategies;
    private readonly Lazy<GitVersionContext> versionContext;
    private GitVersionContext context => this.versionContext.Value;

    public BaseVersionCalculator(ILog log, IRepositoryStore repositoryStore,
        Lazy<GitVersionContext> versionContext, IEnumerable<IVersionStrategy> strategies)
    {
        this.log = log.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.strategies = strategies.ToArray();
        this.versionContext = versionContext.NotNull();
    }

    public (SemanticVersion IncrementedVersion, BaseVersion Version) GetBaseVersion()
    {
        using (this.log.IndentLog("Calculating base versions"))
        {
            var allVersions = new List<(SemanticVersion IncrementedVersion, BaseVersion Version)>();
            foreach (var strategy in this.strategies)
            {
                var baseVersions = GetBaseVersions(strategy).ToList();
                allVersions.AddRange(baseVersions);
            }

            var versions = allVersions.Select(version => new Versions
            {
                IncrementedVersion = version.IncrementedVersion,
                Version = version.Version
            }).ToList();

            FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(versions);

            var maxVersion = versions.Aggregate((v1, v2) => v1.IncrementedVersion > v2.IncrementedVersion ? v1 : v2);
            var matchingVersionsOnceIncremented = versions
                .Where(b => b.Version.BaseVersionSource != null && b.IncrementedVersion == maxVersion.IncrementedVersion)
                .ToList();
            ICommit? latestBaseVersionSource;

            if (matchingVersionsOnceIncremented.Any())
            {
                static Versions CompareVersions(Versions versions1, Versions version2)
                {
                    if (versions1.Version.BaseVersionSource == null)
                    {
                        return version2;
                    }
                    if (version2.Version.BaseVersionSource == null)
                    {
                        return versions1;
                    }

                    return versions1.Version.BaseVersionSource.When < version2.Version.BaseVersionSource.When ? versions1 : version2;
                }

                var latestVersion = matchingVersionsOnceIncremented.Aggregate(CompareVersions);
                latestBaseVersionSource = latestVersion.Version.BaseVersionSource;
                maxVersion = latestVersion;
                log.Info($"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion})," +
                    $" taking oldest source for commit counting ({latestVersion.Version.Source})");
            }
            else
            {
                IEnumerable<Versions> filteredVersions = versions;
                if (!maxVersion.IncrementedVersion.PreReleaseTag!.HasTag())
                {
                    // If the maximal version has no pre-release tag defined than we want to determine just the latest previous
                    // base source which are not comming from pre-release tag.
                    filteredVersions = filteredVersions.Where(v => !v.Version.SemanticVersion.PreReleaseTag!.HasTag());
                }

                Versions version = filteredVersions
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
                maxVersion.Version.BranchNameOverride);

            this.log.Info($"Base version used: {calculatedBase}");

            return new(maxVersion.IncrementedVersion, calculatedBase);
        }
    }

    private IEnumerable<(SemanticVersion IncrementedVersion, BaseVersion Version)> GetBaseVersions(IVersionStrategy strategy)
    {
        foreach (var version in strategy.GetVersions())
        {
            this.log.Info(version.Version.ToString());
            if (strategy is FallbackVersionStrategy || IncludeVersion(version.Version))
            {
                yield return version;
            }
        }
    }

    private bool IncludeVersion(BaseVersion version)
    {
        foreach (var filter in context.FullConfiguration.Ignore.ToFilters())
        {
            if (filter.Exclude(version, out var reason))
            {
                if (reason != null)
                {
                    this.log.Info(reason);
                }
                return false;
            }
        }
        return true;
    }

    private void FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(IEnumerable<Versions> baseVersions)
    {
        if (ReleaseBranchExistsInRepo()) return;

        foreach (var baseVersion in baseVersions)
        {
            if (baseVersion.Version.Source.Contains(MergeMessageVersionStrategy.MergeMessageStrategyPrefix)
                && baseVersion.Version.Source.Contains("Merge branch")
                && baseVersion.Version.Source.Contains("release"))
            {
                if (baseVersion.Version.BaseVersionSource != null)
                {
                    var parents = baseVersion.Version.BaseVersionSource.Parents.ToList();
                    baseVersion.Version = new BaseVersion(
                        baseVersion.Version.Source,
                        baseVersion.Version.ShouldIncrement,
                        baseVersion.Version.SemanticVersion,
                        this.repositoryStore.FindMergeBase(parents[0], parents[1]),
                        baseVersion.Version.BranchNameOverride);
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

    private class Versions
    {
        public SemanticVersion IncrementedVersion { get; set; }
        public BaseVersion Version { get; set; }

        public override string ToString() => $"{Version} | {IncrementedVersion}";
    }
}
