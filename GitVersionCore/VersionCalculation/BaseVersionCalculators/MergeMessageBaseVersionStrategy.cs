namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class MergeMessageBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            // TODO when this approach works, inline the other class into here
            SemanticVersion semanticVersion;
            if (MergeMessageParser.TryParse(context.CurrentCommit, context.Configuration, out semanticVersion))
                return new BaseVersion(true, semanticVersion);

            return null;
        }
    }
}