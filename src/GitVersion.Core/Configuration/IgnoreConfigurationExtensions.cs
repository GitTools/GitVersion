using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.Configuration;

internal static class IgnoreConfigurationExtensions
{
    public static IEnumerable<ITag> Filter(this IIgnoreConfiguration ignore, ITag[] source)
    {
        ignore.NotNull();
        source.NotNull();

        return !ignore.IsEmpty ? source.Where(element => ShouldBeIgnored(element.Commit, ignore)) : source;
    }

    public static IEnumerable<ICommit> Filter(this IIgnoreConfiguration ignore, ICommit[] source)
    {
        ignore.NotNull();
        source.NotNull();

        return !ignore.IsEmpty ? source.Where(element => ShouldBeIgnored(element, ignore)) : source;
    }

    private static bool ShouldBeIgnored(ICommit commit, IIgnoreConfiguration ignore)
        => !ignore.ToFilters().Any(filter => filter.Exclude(commit, out var _));
}
