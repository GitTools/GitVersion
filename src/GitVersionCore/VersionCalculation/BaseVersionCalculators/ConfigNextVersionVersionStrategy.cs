using System.Collections.Generic;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is from NextVersion (the configuration value), unless the current commit is tagged.
    /// BaseVersionSource is null.
    /// Does not increment.
    /// </summary>
    public class ConfigNextVersionVersionStrategy : VersionStrategyBase
    {
        public ConfigNextVersionVersionStrategy(IGitVersionContextFactory gitVersionContextFactory) : base(gitVersionContextFactory)
        {
        }

        public override IEnumerable<BaseVersion> GetVersions()
        {
            var context = ContextFactory.Context;
            if (string.IsNullOrEmpty(context.Configuration.NextVersion) || context.IsCurrentCommitTagged)
                yield break;
            var semanticVersion = SemanticVersion.Parse(context.Configuration.NextVersion, context.Configuration.GitTagPrefix);
            yield return new BaseVersion("NextVersion in GitVersion configuration file", false, semanticVersion, null, null);
        }
    }
}
