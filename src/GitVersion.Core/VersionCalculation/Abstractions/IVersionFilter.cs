using GitVersion.Git;

namespace GitVersion.VersionCalculation;

public interface IVersionFilter
{
    bool Exclude(IBaseVersion baseVersion, out string? reason);
    bool Exclude(ICommit commit, out string? reason);
}
