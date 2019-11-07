using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation
{
    public class BaseVersionCalculator : IBaseVersionCalculator
    {
        private readonly ILog log;
        private readonly IVersionStrategy[] strategies;

        public BaseVersionCalculator(ILog log, IEnumerable<IVersionStrategy> strategies)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.strategies = strategies?.ToArray() ?? Array.Empty<IVersionStrategy>();
        }

        public BaseVersion GetBaseVersion(GitVersionContext context)
        {
            using (log.IndentLog("Calculating base versions"))
            {
                var baseVersions = strategies
                    .SelectMany(s => s.GetVersions(context))
                    .Where(v =>
                    {
                        if (v == null) return false;

                        log.Info(v.ToString());

                        foreach (var filter in context.Configuration.VersionFilters)
                        {
                            if (filter.Exclude(v, out var reason))
                            {
                                log.Info(reason);
                                return false;
                            }
                        }

                        return true;
                    })
                    .Select(v => new Versions
                    {
                        IncrementedVersion = MaybeIncrement(context, v),
                        Version = v
                    })
                    .ToList();

                FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted
                    (context, baseVersions);
                var maxVersion = baseVersions.Aggregate((v1, v2) => v1.IncrementedVersion > v2.IncrementedVersion ? v1 : v2);
                var matchingVersionsOnceIncremented = baseVersions
                    .Where(b => b.Version.BaseVersionSource != null && b.IncrementedVersion == maxVersion.IncrementedVersion)
                    .ToList();
                BaseVersion baseVersionWithOldestSource;
                if (matchingVersionsOnceIncremented.Any())
                {
                    var oldest = matchingVersionsOnceIncremented.Aggregate((v1, v2) => v1.Version.BaseVersionSource.Committer.When < v2.Version.BaseVersionSource.Committer.When ? v1 : v2);
                    baseVersionWithOldestSource = oldest.Version;
                    maxVersion = oldest;
                    log.Info($"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion}), taking oldest source for commit counting ({baseVersionWithOldestSource.Source})");
                }
                else
                {
                    baseVersionWithOldestSource = baseVersions
                        .Where(v => v.Version.BaseVersionSource != null)
                        .OrderByDescending(v => v.IncrementedVersion)
                        .ThenByDescending(v => v.Version.BaseVersionSource.Committer.When)
                        .First()
                        .Version;
                }

                if (baseVersionWithOldestSource.BaseVersionSource == null)
                    throw new Exception("Base version should not be null");

                var calculatedBase = new BaseVersion(
                    context, maxVersion.Version.Source, maxVersion.Version.ShouldIncrement, maxVersion.Version.SemanticVersion,
                    baseVersionWithOldestSource.BaseVersionSource, maxVersion.Version.BranchNameOverride);

                log.Info($"Base version used: {calculatedBase}");

                return calculatedBase;
            }
        }

        public static SemanticVersion MaybeIncrement(GitVersionContext context, BaseVersion version)
        {
            var increment = IncrementStrategyFinder.DetermineIncrementedField(context, version);
            if (increment != null)
            {
                return version.SemanticVersion.IncrementVersion(increment.Value);
            }

            return version.SemanticVersion;
        }

        private void FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(
            GitVersionContext context, List<Versions> baseVersions)
        {
            if (!ReleaseBranchExistsInRepo(context))
            {
                foreach (var baseVersion in baseVersions)
                {
                    if (baseVersion.Version.Source.Contains(
                        MergeMessageVersionStrategy.MergeMessageStrategyPrefix)
                        && baseVersion.Version.Source.Contains("Merge branch")
                        && baseVersion.Version.Source.Contains("release"))
                    {
                        var parents = baseVersion.Version.BaseVersionSource.Parents.ToList();
                        baseVersion.Version = new BaseVersion(
                            context,
                            baseVersion.Version.Source,
                            baseVersion.Version.ShouldIncrement,
                            baseVersion.Version.SemanticVersion,
                            context.Repository.ObjectDatabase.FindMergeBase(parents[0], parents[1]),
                            baseVersion.Version.BranchNameOverride);
                    }
                }
            }
        }

        private bool ReleaseBranchExistsInRepo(GitVersionContext context)
        {
            var releaseBranchConfig = context.FullConfiguration.Branches
                .Where(b => b.Value.IsReleaseBranch == true)
                .ToList();
            var releaseBranches = context.Repository.Branches
                    .Where(b => releaseBranchConfig.Any(c => Regex.IsMatch(b.FriendlyName, c.Value.Regex)));
            return releaseBranches.Any();
        }

        private class Versions
        {
            public SemanticVersion IncrementedVersion { get; set; }
            public BaseVersion Version { get; set; }

            public override string ToString()
            {
                return $"{Version} | {IncrementedVersion}";
            }
        }
    }
}
