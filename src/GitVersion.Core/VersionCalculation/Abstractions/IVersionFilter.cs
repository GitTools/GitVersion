using GitVersion.Git;

namespace GitVersion.VersionCalculation;

/// <summary>Determines whether a base version or commit should be excluded from version calculation.</summary>
public interface IVersionFilter
{
    /// <summary>Returns <see langword="true"/> when <paramref name="baseVersion"/> should be excluded, setting <paramref name="reason"/> to a description of why.</summary>
    bool Exclude(IBaseVersion baseVersion, out string? reason);

    /// <summary>Returns <see langword="true"/> when <paramref name="commit"/> should be excluded, setting <paramref name="reason"/> to a description of why.</summary>
    bool Exclude(ICommit? commit, out string? reason);
}
