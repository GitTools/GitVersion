namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class ConfigNextVersionBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            if (string.IsNullOrEmpty(context.Configuration.NextVersion))
                return null;
            return new BaseVersion(false, SemanticVersion.Parse(context.Configuration.NextVersion, context.Configuration.GitTagPrefix));
        }
    }
}