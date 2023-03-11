using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public static class IgnoreConfigurationExtensions
{
    public static IEnumerable<IVersionFilter> ToFilters(this IIgnoreConfiguration source)
    {
        source.NotNull();

        if (source.Shas.Any()) yield return new ShaVersionFilter(source.Shas);
        if (source.Before.HasValue) yield return new MinDateVersionFilter(source.Before.Value);
    }
}
