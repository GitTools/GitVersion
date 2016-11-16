namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Version is from NextVersion (the configuration value), unless the current commit is tagged.
    /// BaseVersionSource is null.
    /// Does not increment.
    /// </summary>
    public class ConfigNextVersionBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var configToUse = context.Configurations.First();
            if (string.IsNullOrEmpty(configToUse.NextVersion) || context.IsCurrentCommitTagged)
                yield break;
            var semanticVersion = SemanticVersion.Parse(configToUse.NextVersion, configToUse.GitTagPrefix);
            yield return new BaseVersion("NextVersion in GitVersion configuration file", false, semanticVersion, null, null);
        }
    }
}