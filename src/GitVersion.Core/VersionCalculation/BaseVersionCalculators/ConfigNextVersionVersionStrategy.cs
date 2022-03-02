using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

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
        var nextVersion = Context.Configuration?.NextVersion;
        if (nextVersion.IsNullOrEmpty() || Context.IsCurrentCommitTagged)
            yield break;
        var semanticVersion = SemanticVersion.Parse(nextVersion, Context.Configuration?.GitTagPrefix);
        yield return new BaseVersion("NextVersion in GitVersion configuration file", false, semanticVersion, null, null);
    }
}
