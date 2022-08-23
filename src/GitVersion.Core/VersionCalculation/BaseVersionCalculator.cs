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

    public BaseVersionCalculator(ILog log, IRepositoryStore repositoryStore, Lazy<GitVersionContext> versionContext, IEnumerable<IVersionStrategy> strategies)
    {
        this.log = log.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
        this.strategies = strategies.ToArray();
        this.versionContext = versionContext.NotNull();
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
                .Select(baseVersion => new Versions { IncrementedVersion = this.repositoryStore.MaybeIncrement(baseVersion, context), Version = baseVersion })
                .ToList();

            FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(versions);

            if (context.Configuration.VersioningMode == VersioningMode.Mainline)
            {
                versions = versions
                    .Where(b => b.IncrementedVersion.PreReleaseTag?.HasTag() != true)
                    .ToList();
            }

            var maxVersion = versions.Aggregate((v1, v2) => v1.IncrementedVersion > v2.IncrementedVersion ? v1 : v2);
            var matchingVersionsOnceIncremented = versions
                .Where(b => b.Version.BaseVersionSource != null && b.IncrementedVersion == maxVersion.IncrementedVersion)
                .ToList();
            BaseVersion baseVersionWithOldestSource;

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

                var oldest = matchingVersionsOnceIncremented.Aggregate(CompareVersions);
                baseVersionWithOldestSource = oldest.Version;
                maxVersion = oldest;
                this.log.Info($"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion}), taking oldest source for commit counting ({baseVersionWithOldestSource.Source})");
            }
            else
            {
                baseVersionWithOldestSource = versions
                    .Where(v => v.Version.BaseVersionSource != null)
                    .OrderByDescending(v => v.IncrementedVersion)
                    .ThenByDescending(v => v.Version.BaseVersionSource?.When)
                    .First()
                    .Version;
            }

            if (baseVersionWithOldestSource.BaseVersionSource == null)
                throw new Exception("Base version should not be null");

            var calculatedBase = new BaseVersion(
                maxVersion.Version.Source,
                maxVersion.Version.ShouldIncrement,
                maxVersion.Version.SemanticVersion,
                baseVersionWithOldestSource.BaseVersionSource,
                maxVersion.Version.BranchNameOverride);

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

            yield return version;
        }
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
