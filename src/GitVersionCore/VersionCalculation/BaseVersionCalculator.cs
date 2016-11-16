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
                    .SelectMany(s =>
                    // TODO Return the configuration used.
                    s.GetVersions(context).Select(version => Tuple.Create(s, version)))
                    .Where(v =>
                    {
                        if (v == null || v.Item2 == null)
                            return false;

                        Logger.WriteInfo(v.ToString());

                        foreach (var config in context.Configurations)
                        {
                            foreach (var filter in config.VersionFilters)
                            {
                                string reason;
                                if (filter.Exclude(v.Item2, out reason))
                                {
                                    Logger.WriteInfo(reason);
                                    return false;
                                }
                            }
                        }

                        return true;
                    })
                    .Select(v => new
                    {
                        IncrementedVersion = MaybeIncrement(context, v.Item2),
                        Version = v,
                        Source = v.Item2.Source,
                        Strategy = v.Item1
                    })
                    .ToList();

                var maxVersion = baseVersions.Aggregate((v1, v2) => v1.IncrementedVersion > v2.IncrementedVersion ? v1 : v2);
                var matchingVersionsOnceIncremented = baseVersions
                    .Where(b => b.Version.Item2.BaseVersionSource != null && b.IncrementedVersion == maxVersion.IncrementedVersion)
                    .ToList();
                BaseVersion baseVersionWithOldestSource;
                if (matchingVersionsOnceIncremented.Any())
                {
                    baseVersionWithOldestSource = matchingVersionsOnceIncremented.Aggregate((v1, v2) => v1.Version.Item2.BaseVersionSource.Committer.When < v2.Version.Item2.BaseVersionSource.Committer.When ? v1 : v2).Version.Item2;
                    Logger.WriteInfo(string.Format(
                        "Found multiple base versions which will produce the same SemVer ({0}), taking oldest source for commit counting ({1})",
                        maxVersion.IncrementedVersion,
                        baseVersionWithOldestSource.Source));
                }
                else
                {
                    baseVersionWithOldestSource = baseVersions
                        .Where(v => v.Version.Item2.BaseVersionSource != null)
                        .OrderByDescending(v => v.IncrementedVersion)
                        .ThenByDescending(v => v.Version.Item2.BaseVersionSource.Committer.When)
                        .First()
                        .Version.Item2;
                }

                if (baseVersionWithOldestSource.BaseVersionSource == null)
                    throw new Exception("Base version should not be null");

                var calculatedBase = new BaseVersion(
                    maxVersion.Version.Item2.Source, maxVersion.Version.Item2.ShouldIncrement, maxVersion.Version.Item2.SemanticVersion,
                    baseVersionWithOldestSource.BaseVersionSource, maxVersion.Version.Item2.BranchNameOverride);

                Logger.WriteInfo(string.Format("Base version used: {0}", calculatedBase));

                // TODO Return the configuration used.
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