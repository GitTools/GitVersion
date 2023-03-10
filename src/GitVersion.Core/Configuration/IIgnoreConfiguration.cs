using GitVersion.VersionCalculation;

namespace GitVersion.Configuration
{
    public interface IIgnoreConfiguration
    {
        DateTimeOffset? Before { get; }

        IReadOnlyList<string> Shas { get; }

        IEnumerable<IVersionFilter> ToFilters();

        bool IsEmpty { get; }
    }
}
