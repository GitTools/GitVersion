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
                            if (filter.Exclude(v, context.Repository, out reason))
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
                    .Where(b => b.IncrementedVersion == maxVersion.IncrementedVersion)
                    .ToList();
                BaseVersion baseVersionWithOldestSource;
                if (matchingVersionsOnceIncremented.Any(b => b != maxVersion))
                {
                    var oldest = matchingVersionsOnceIncremented.Aggregate((v1, v2) => v1.Version.Source.Commit.DistanceFromTip > v2.Version.Source.Commit.DistanceFromTip ? v1 : v2);
                    baseVersionWithOldestSource = oldest.Version;
                    maxVersion = oldest;
                    Logger.WriteInfo(string.Format(
                        "Found multiple base versions which will produce the same SemVer ({0}), taking source with largest distance from tip ({1})",
                        maxVersion.IncrementedVersion,
                        baseVersionWithOldestSource.Source.Description));
                }
                else
                {
                    baseVersionWithOldestSource = baseVersions
                        .OrderByDescending(v => v.IncrementedVersion)
                        .ThenByDescending(v => v.Version.Source.Commit.When)
                        .First()
                        .Version;
                }

                var calculatedBase = new BaseVersion(
                    context, maxVersion.Version.ShouldIncrement, maxVersion.Version.SemanticVersion,
                    baseVersionWithOldestSource.Source, maxVersion.Version.BranchNameOverride);

                Logger.WriteInfo(string.Format("Base version used: {0}", calculatedBase));

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
    }
}