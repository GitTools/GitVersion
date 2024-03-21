using GitVersion.Extensions;

namespace GitVersion.Git;

public static class CommitExtensions
{
    public static bool IsMergeCommit(this ICommit source) => source.NotNull().Parents.Count >= 2;
}
