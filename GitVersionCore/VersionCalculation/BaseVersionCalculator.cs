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
            return strategies
                .Select(s => s.GetVersion(context))
                .Where(v => v != null)
                .Aggregate((v1, v2) =>
                {
                    if (v1.SemanticVersion > v2.SemanticVersion)
                    {
                        return new BaseVersion(v1.ShouldIncrement, v1.SemanticVersion, v1.BaseVersionSource ?? v2.BaseVersionSource);
                    }

                    return new BaseVersion(v2.ShouldIncrement, v2.SemanticVersion, v2.BaseVersionSource ?? v1.BaseVersionSource);
                });
        }
    }
}