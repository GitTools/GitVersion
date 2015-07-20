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
                        if (v != null)
                        {
                            Logger.WriteInfo(v.ToString());
                            return true;
                        }

                        return false;
                    })
                    .ToList();

                var maxVersion = baseVersions.Aggregate((v1, v2) =>
                {
                    if (v1.SemanticVersion > v2.SemanticVersion)
                    {
                        return new BaseVersion(v1.Source, v1.ShouldIncrement, v1.SemanticVersion, v1.BaseVersionSource ?? v2.BaseVersionSource, v1.BranchNameOverride);
                    }
                    return new BaseVersion(v2.Source, v2.ShouldIncrement, v2.SemanticVersion, v2.BaseVersionSource ?? v1.BaseVersionSource, v2.BranchNameOverride);
                });
                var incrementedMax = MaybeIncrement(context, maxVersion);
                var matchingVersionsOnceIncremented = baseVersions.Where(b => b.BaseVersionSource != null && MaybeIncrement(context, b) == incrementedMax).ToList();
                BaseVersion baseVersionWithOldestSource;
                if (matchingVersionsOnceIncremented.Any())
                {
                    baseVersionWithOldestSource = matchingVersionsOnceIncremented.Aggregate((v1, v2) => v1.BaseVersionSource.Committer.When < v2.BaseVersionSource.Committer.When ? v1 : v2);
                    Logger.WriteInfo(string.Format(
                        "Found multiple base versions which will produce the same SemVer ({0}), taking oldest source for commit counting ({1})",
                        incrementedMax,
                        baseVersionWithOldestSource.Source));
                }
                else
                {
                    baseVersionWithOldestSource = maxVersion;
                }

                if (baseVersionWithOldestSource.BaseVersionSource == null)
                    throw new Exception("Base version should not be null");

                var calculatedBase = new BaseVersion(
                    maxVersion.Source, maxVersion.ShouldIncrement, maxVersion.SemanticVersion,
                    baseVersionWithOldestSource.BaseVersionSource, maxVersion.BranchNameOverride);

                Logger.WriteInfo(string.Format("Base version used: {0}", calculatedBase));

                return calculatedBase;
            }
        }

        static SemanticVersion MaybeIncrement(GitVersionContext context, BaseVersion version)
        {
            return version.ShouldIncrement ? version.SemanticVersion.IncrementVersion(context.Configuration.Increment) : version.SemanticVersion;
        }
    }
}