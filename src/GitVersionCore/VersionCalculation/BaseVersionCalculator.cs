namespace GitVersion.VersionCalculation
{
    using System;
    using System.Linq;
    using GitVersion.VersionCalculation.BaseVersionCalculators;

    public class BaseVersionCalculator : IBaseVersionCalculator
    {
        readonly BaseVersionStrategy[] strategies;

        public BaseVersionCalculator(params BaseVersionStrategy[] strategies)
        {
            this.strategies = strategies;
        }

        public BaseVersion GetBaseVersion(GitVersionContext context)
        {
            using (Logger.IndentLog("Calculating base versions"))
            {
                var baseVersions = strategies
                    .SelectMany(s => s.GetVersions(context))
                    .Where(v =>
                    {
                        if (v == null) return false;

                        Logger.WriteInfo(v.ToString());

                        foreach (var filter in context.Configuration.VersionFilters)
                        {
                            string reason;
                            if (filter.Exclude(v, out reason))
                            {
                                Logger.WriteInfo(reason);
                                return false;
                            }
                        }

                        return true;
                    })
                    .Select(v => new
                    {
                        IncrementedVersion = MaybeIncrement(context, v),
                        Version = v
                    })
                    .ToList();

                var maxVersion = baseVersions.Aggregate((v1, v2) => v1.IncrementedVersion > v2.IncrementedVersion ? v1 : v2);
                var matchingVersionsOnceIncremented = baseVersions
                    .Where(b => b.Version.BaseVersionSource != null && b.IncrementedVersion == maxVersion.IncrementedVersion)
                    .ToList();
                BaseVersion baseVersionWithOldestSource;
                if (matchingVersionsOnceIncremented.Any())
                {
                    baseVersionWithOldestSource = matchingVersionsOnceIncremented.Aggregate((v1, v2) => v1.Version.BaseVersionSource.Committer.When < v2.Version.BaseVersionSource.Committer.When ? v1 : v2).Version;
                    Logger.WriteInfo(string.Format(
                        "Found multiple base versions which will produce the same SemVer ({0}), taking oldest source for commit counting ({1})",
                        maxVersion.IncrementedVersion,
                        baseVersionWithOldestSource.Source));
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

                Logger.WriteInfo(string.Format("Base version used: {0}", calculatedBase));

                return calculatedBase;
            }
        }

        static SemanticVersion MaybeIncrement(GitVersionContext context, BaseVersion version)
        {
            var increment = IncrementStrategyFinder.DetermineIncrementedField(context, version);
            if (increment != null)
            {
                return version.SemanticVersion.IncrementVersion(increment.Value);
            }

            return version.SemanticVersion;
        }
    }
}