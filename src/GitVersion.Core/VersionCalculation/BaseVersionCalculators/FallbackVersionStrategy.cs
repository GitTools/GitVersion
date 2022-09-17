using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is 0.1.0.
/// BaseVersionSource is the "root" commit reachable from the current commit.
/// Does not increment.
/// </summary>
public class FallbackVersionStrategy : IVersionStrategy
{
    public virtual IEnumerable<BaseVersion> GetVersions(IBranch branch, EffectiveConfiguration configuration)
    {
        yield return new BaseVersion("Fallback base version", true, new SemanticVersion(), null, null);
    }
}
