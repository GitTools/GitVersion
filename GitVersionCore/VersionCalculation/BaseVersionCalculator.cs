namespace GitVersion.VersionCalculation
{
    using System.Linq;
    using BaseVersionCalculators;

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
                var baseVersion = strategies
                    .Select(s => s.GetVersion(context))
                    .Where(v =>
                    {
                        if (v != null)
                        {
                            Logger.WriteInfo(v.ToString());
                            return true;
                        }

                        return false;
                    })
                    .Aggregate((v1, v2) =>
                    {
                        if (v1.SemanticVersion > v2.SemanticVersion)
                        {
                            return new BaseVersion(v1.Source, v1.ShouldIncrement, v1.SemanticVersion, v1.BaseVersionSource ?? v2.BaseVersionSource, v1.BranchNameOverride);
                        }

                        return new BaseVersion(v2.Source, v2.ShouldIncrement, v2.SemanticVersion, v2.BaseVersionSource ?? v1.BaseVersionSource, v2.BranchNameOverride);
                    });

                Logger.WriteInfo(string.Format("Base version used: {0}", baseVersion));

                return baseVersion;
            }
        }
    }
}