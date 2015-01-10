namespace GitVersion.VersionCalculation
{
    using System.Linq;
    using GitVersion.VersionCalculation.BaseVersionCalculators;

    public class BaseVersionCalculator
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
                .Aggregate((v1, v2) => v1.SemanticVersion > v2.SemanticVersion ? v1 : v2);
        }
    }
}