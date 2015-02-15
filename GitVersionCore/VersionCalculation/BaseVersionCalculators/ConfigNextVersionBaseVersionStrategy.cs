namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class ConfigNextVersionBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            if (string.IsNullOrEmpty(context.Configuration.NextVersion) || context.IsCurrentCommitTagged)
                return null;
            var semanticVersion = SemanticVersion.Parse(context.Configuration.NextVersion, context.Configuration.GitTagPrefix);
            return new BaseVersion("NextVersion in GitVersionConfig.yaml", false, semanticVersion, null, null);
        }
    }
}