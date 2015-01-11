namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class MergeMessageBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            foreach (var commit in context.CurrentBranch.CommitsPriorToThan(context.CurrentCommit.When()))
            {
                SemanticVersion semanticVersion;
                // TODO when this approach works, inline the other class into here
                if (MergeMessageParser.TryParse(context.CurrentCommit, context.Configuration, out semanticVersion))
                    return new BaseVersion(true, true, semanticVersion, commit);
            }
            return null;
        }
    }
}