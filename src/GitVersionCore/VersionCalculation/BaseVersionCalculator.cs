using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Logging;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionCalculation
{
    public class BaseVersionCalculator : IBaseVersionCalculator
    {
        private readonly ILog log;
        private readonly IRepository repository;
        private readonly GitVersionContext context;
        private readonly IVersionStrategy[] strategies;

        public BaseVersionCalculator(ILog log, IRepository repository, IOptions<GitVersionContext> versionContext, IEnumerable<IVersionStrategy> strategies)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.strategies = strategies?.ToArray() ?? Array.Empty<IVersionStrategy>();
            context = versionContext.Value;
        }

        public BaseVersion GetBaseVersion()
        {
            using (log.IndentLog("Calculating base versions"))
            {
                var baseVersions = strategies
                    .SelectMany(s => s.GetVersions())
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
                        IncrementedVersion = MaybeIncrement(v),
                        Version = v
                    })
                    .ToList();

                FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(baseVersions);
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
                    maxVersion.Version.Source, maxVersion.Version.ShouldIncrement, maxVersion.Version.SemanticVersion,
                    baseVersionWithOldestSource.BaseVersionSource, maxVersion.Version.BranchNameOverride);

                log.Info($"Base version used: {calculatedBase}");

                return calculatedBase;
            }
        }

        private void FixTheBaseVersionSourceOfMergeMessageStrategyIfReleaseBranchWasMergedAndDeleted(IEnumerable<Versions> baseVersions)
        {
            if (!ReleaseBranchExistsInRepo())
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
                            baseVersion.Version.Source,
                            baseVersion.Version.ShouldIncrement,
                            baseVersion.Version.SemanticVersion,
                            repository.ObjectDatabase.FindMergeBase(parents[0], parents[1]),
                            baseVersion.Version.BranchNameOverride);
                    }
                }
            }
        }

        public SemanticVersion MaybeIncrement(BaseVersion baseVersion)
        {
            var increment = IncrementStrategyFinder.DetermineIncrementedField(repository, context, baseVersion);
            return increment != null ? baseVersion.SemanticVersion.IncrementVersion(increment.Value) : baseVersion.SemanticVersion;
        }

        private bool ReleaseBranchExistsInRepo()
        {
            var releaseBranchConfig = context.FullConfiguration.Branches
                .Where(b => b.Value.IsReleaseBranch == true)
                .ToList();
            var releaseBranches = repository.Branches
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
