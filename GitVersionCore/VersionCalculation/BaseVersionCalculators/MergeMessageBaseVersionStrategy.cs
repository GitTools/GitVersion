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
                        return new[]
                        {
                            new BaseVersion(true, true, semanticVersion, c)
                        };
                    return Enumerable.Empty<BaseVersion>();
                })
                .ToArray();

            return baseVersions.Length > 1 ? baseVersions.Aggregate((x, y) => x.SemanticVersion > y.SemanticVersion ? x : y) : baseVersions.SingleOrDefault();
        }
    }
}