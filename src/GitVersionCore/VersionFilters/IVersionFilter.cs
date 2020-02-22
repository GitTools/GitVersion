using GitVersion.VersionCalculation;

namespace GitVersion.VersionFilters
{
    public interface IVersionFilter
    {
        bool Exclude(BaseVersion version, out string reason);
    }
}
