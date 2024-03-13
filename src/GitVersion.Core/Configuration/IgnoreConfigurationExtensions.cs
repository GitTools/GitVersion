using GitVersion.Extensions;

namespace GitVersion.Configuration;

public static class IgnoreConfigurationExtensions
{
    public static IEnumerable<ITag> Filter(this IIgnoreConfiguration ignore, IEnumerable<ITag> source)
    {
        ignore.NotNull();
        source.NotNull();

        if (!ignore.IsEmpty)
        {
            return source.Where(element => ShouldBeIgnored(element.Commit, ignore));
        }
        return source;
    }

    public static IEnumerable<ICommit> Filter(this IIgnoreConfiguration ignore, IEnumerable<ICommit> source)
    {
        ignore.NotNull();
        source.NotNull();

        if (!ignore.IsEmpty)
        {
            return source.Where(element => ShouldBeIgnored(element, ignore));
        }
        return source;
    }

    private static bool ShouldBeIgnored(ICommit commit, IIgnoreConfiguration ignore)
        => !(commit.When <= ignore.Before) && !ignore.Shas.Contains(commit.Sha);
}
