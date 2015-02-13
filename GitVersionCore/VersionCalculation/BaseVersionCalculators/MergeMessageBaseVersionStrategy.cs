namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System.Linq;

    public class MergeMessageBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            var commitsPriorToThan = context.CurrentBranch
                .CommitsPriorToThan(context.CurrentCommit.When());
            var baseVersions = commitsPriorToThan
                .SelectMany(c =>
                {
                    SemanticVersion semanticVersion;
                    // TODO when this approach works, inline the other class into here
                    if (MergeMessageParser.TryParse(c, context.Configuration, out semanticVersion))
                    {
                        var shouldIncrement = !context.Configuration.PreventIncrementForMergedBranchVersion;
                        return new[]
                        {
                            new BaseVersion(string.Format("Merge message '{0}'", c.Message.Trim()), shouldIncrement, true, semanticVersion, c, null)
                        };
                    }
                    return Enumerable.Empty<BaseVersion>();
                })
                .ToArray();

            return baseVersions.Length > 1 ? baseVersions.Aggregate((x, y) => x.SemanticVersion > y.SemanticVersion ? x : y) : baseVersions.SingleOrDefault();
        }
    }
}