namespace GitVersion.VersionCalculation;

public interface IVersionFilter
{
    bool Exclude(IBaseVersion baseVersion, out string? reason);
}
