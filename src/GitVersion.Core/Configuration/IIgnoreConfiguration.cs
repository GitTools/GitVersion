using GitVersion.VersionCalculation;

namespace GitVersion.Configuration
{
    public interface IIgnoreConfiguration
    {
        DateTimeOffset? Before { get; }

        IReadOnlyList<string> Shas { get; }

        public IEnumerable<IVersionFilter> ToFilters()
        {
            if (Shas.Any()) yield return new ShaVersionFilter(Shas);
            if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
        }

        public bool IsEmpty => Before == null && !Shas.Any();
    }
}
