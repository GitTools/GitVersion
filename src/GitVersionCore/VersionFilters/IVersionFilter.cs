using GitVersion.VersionCalculation.BaseVersionCalculators;
using LibGit2Sharp;

namespace GitVersion.VersionFilters
{
    public interface IVersionFilter
    {
        bool Exclude(BaseVersion version, IRepository repository, out string reason);
    }
}
