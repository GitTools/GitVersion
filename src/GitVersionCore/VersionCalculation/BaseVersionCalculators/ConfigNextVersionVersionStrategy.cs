using System.Collections.Generic;

namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    /// <summary>
    /// Version is from NextVersion (the configuration value), unless the current commit is tagged.
    /// BaseVersionSource is null.
    /// Does not increment.
    /// </summary>
    public class ConfigNextVersionVersionStrategy : IVersionStrategy
    {
        public virtual IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            if (string.IsNullOrEmpty(context.Configuration.NextVersion) || context.IsCurrentCommitTagged)
                yield break;
            var semanticVersion = SemanticVersion.Parse(context.Configuration.NextVersion, context.Configuration.GitTagPrefix);
            yield return new BaseVersion(context, "NextVersion in GitVersion configuration file", false, semanticVersion, null, null);
        }
    }
}
