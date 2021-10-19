using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

public class BaseVersionCalculator : IBaseVersionCalculator
{
    private readonly ILog log;
    private readonly IRepositoryStore repositoryStore;
    private readonly IVersionStrategy[] strategies;
    private readonly Lazy<GitVersionContext> versionContext;
    private GitVersionContext context => this.versionContext.Value;

    public BaseVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext, IEnumerable<IVersionStrategy> strategies)
    {
        this.log = log ?? throw new ArgumentNullException(nameof(log));
        this.repositoryStore = repositoryStore ?? throw new ArgumentNullException(nameof(repositoryStore));
        this.strategies = strategies?.ToArray() ?? Array.Empty<IVersionStrategy>();
        this.versionContext = versionContext ?? throw new ArgumentNullException(nameof(versionContext));
    }

    public BaseVersion GetBaseVersion()
    {
        using (this.log.IndentLog("Calculating base versions"))
        {
            var allVersions = new List<BaseVersion>();
            foreach (var strategy in this.strategies)
            {
                var baseVersions = GetBaseVersions(strategy).ToList();
                allVersions.AddRange(baseVersions);
            }

            var versions = allVersions
                .Select(baseVersion => new Versions
                {
                    IncrementedVersion = this.repositoryStore.MaybeIncrement(baseVersion, context),
                    Version = baseVersion
                })
                .ToList();

            FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(versions);

            if (context.Configuration?.VersioningMode == VersioningMode.Mainline)
            {
                versions = versions
                    .Where(b => b.IncrementedVersion?.PreReleaseTag?.HasTag() != true)
                    .ToList();
            }

            var maxVersion = versions.Aggregate((v1, v2) => v1.IncrementedVersion! > v2.IncrementedVersion! ? v1 : v2);
            var matchingVersionsOnceIncremented = versions
                .Where(b => b.Version?.BaseVersionSource != null && b.IncrementedVersion == maxVersion.IncrementedVersion)
                .ToList();
            BaseVersion baseVersionWithOldestSource;
            if (matchingVersionsOnceIncremented.Any())
            {
                var oldest = matchingVersionsOnceIncremented.Aggregate((v1, v2) =>
                    v1.Version!.BaseVersionSource!.When < v2.Version!.BaseVersionSource!.When ? v1 : v2);
                baseVersionWithOldestSource = oldest!.Version!;
                maxVersion = oldest;
                this.log.Info($"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion}), taking oldest source for commit counting ({baseVersionWithOldestSource!.Source})");
            }
            else
            {
                baseVersionWithOldestSource = versions
                    .Where(v => v.Version?.BaseVersionSource != null)
                    .OrderByDescending(v => v.IncrementedVersion)
                    .ThenByDescending(v => v.Version!.BaseVersionSource!.When)
                    .First()
                    .Version!;
            }

            if (baseVersionWithOldestSource.BaseVersionSource == null)
                throw new Exception("Base version should not be null");

            var calculatedBase = new BaseVersion(
                maxVersion.Version!.Source, maxVersion.Version.ShouldIncrement, maxVersion.Version.SemanticVersion,
                baseVersionWithOldestSource.BaseVersionSource, maxVersion.Version.BranchNameOverride);

            this.log.Info($"Base version used: {calculatedBase}");

            return calculatedBase;
        }
    }
    private IEnumerable<BaseVersion> GetBaseVersions(IVersionStrategy strategy)
    {
        foreach (var version in strategy.GetVersions())
        {
            if (version == null) continue;

            this.log.Info(version.ToString());
            if (strategy is FallbackVersionStrategy || IncludeVersion(version))
            {
                yield return version;
            }
        }
    }
    private bool IncludeVersion(BaseVersion version)
    {
        if (context.Configuration == null)
            return false;

        foreach (var filter in context.Configuration.VersionFilters)
        {
            if (filter.Exclude(version, out var reason))
            {
                this.log.Info(reason);
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
            if (baseVersion.Version?.Source.Contains(MergeMessageVersionStrategy.MergeMessageStrategyPrefix) == true
                && baseVersion.Version.Source.Contains("Merge branch")
                && baseVersion.Version.Source.Contains("release"))
            {
                var parents = baseVersion.Version.BaseVersionSource!.Parents.ToList();
                baseVersion.Version = new BaseVersion(
                    baseVersion.Version.Source,
                    baseVersion.Version.ShouldIncrement,
                    baseVersion.Version.SemanticVersion,
                    this.repositoryStore.FindMergeBase(parents[0], parents[1]),
                    baseVersion.Version.BranchNameOverride);
            }
        }
    }

    private bool ReleaseBranchExistsInRepo()
    {
        var releaseBranchConfig = context.FullConfiguration?.GetReleaseBranchConfig();
        var releaseBranches = this.repositoryStore.GetReleaseBranches(releaseBranchConfig);
        return releaseBranches.Any();
    }

    private class Versions
    {
        public SemanticVersion? IncrementedVersion { get; set; }
        public BaseVersion? Version { get; set; }

        public override string ToString() => $"{Version} | {IncrementedVersion}";
    }
}
