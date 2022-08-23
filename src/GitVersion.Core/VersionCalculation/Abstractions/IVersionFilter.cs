namespace GitVersion.VersionCalculation;

public interface IVersionFilter
{
    bool Exclude(ICommit commit, out string? reason);
}
