using System.Collections.Generic;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is from NextVersion (the configuration value), unless the current commit is tagged.
    /// BaseVersionSource is null.
    /// Does not increment.
    /// </summary>
    public class ConfigNextVersionVersionStrategy : VersionStrategyBase
    {
        public ConfigNextVersionVersionStrategy(IRepository repository, IOptions<GitVersionContext> versionContext) : base(repository, versionContext)
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
