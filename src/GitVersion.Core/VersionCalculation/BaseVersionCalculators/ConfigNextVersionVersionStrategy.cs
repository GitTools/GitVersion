using System;
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
        public ConfigNextVersionVersionStrategy(Lazy<GitVersionContext> versionContext) : base(versionContext)
        {
        }

        public override IEnumerable<BaseVersion> GetVersions()
        {
            if (string.IsNullOrEmpty(Context.Configuration.NextVersion) || Context.IsCurrentCommitTagged)
                yield break;
            var semanticVersion = SemanticVersion.Parse(Context.Configuration.NextVersion, Context.Configuration.GitTagPrefix);
            yield return new BaseVersion("NextVersion in GitVersion configuration file", false, semanticVersion, null, null);
        }
    }
}
