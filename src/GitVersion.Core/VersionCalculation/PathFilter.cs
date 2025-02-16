using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal class PathFilter(IGitRepository repository, GitVersionContext context, IEnumerable<string> paths, PathFilter.PathFilterMode mode = PathFilter.PathFilterMode.Inclusive) : IVersionFilter
{
    public enum PathFilterMode { Inclusive = 0, Exclusive = 1 }

    private readonly IEnumerable<string> paths = paths.NotNull();
    private readonly PathFilterMode mode = mode;
    private readonly GitVersionContext context = context;

    public bool Exclude(IBaseVersion baseVersion, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(baseVersion);

        reason = null;
        if (baseVersion.SourceType != VersionIncrementSourceType.Tree) return false;

        return Exclude(baseVersion.BaseVersionSource, out reason);
    }

    public bool Exclude(ICommit? commit, out string? reason)
    {
        commit.NotNull();
        reason = null;

        if (commit != null)
        {
            var patchPaths = repository.FindPatchPaths(commit, context.Configuration.TagPrefixPattern);

            if (patchPaths != null)
            {
                switch (mode)
                {
                    case PathFilterMode.Inclusive:
                        if (!paths.Any(path => patchPaths.Any(p => p.StartsWith(path, StringComparison.OrdinalIgnoreCase))))
                        {
                            reason = "Source was ignored due to commit path is not present";
                            return true;
                        }
                        break;
                    case PathFilterMode.Exclusive:
                        if (paths.Any(path => patchPaths.All(p => p.StartsWith(path, StringComparison.OrdinalIgnoreCase))))
                        {
                            reason = "Source was ignored due to commit path excluded";
                            return true;
                        }
                        break;
                }
            }
        }

        return false;
    }
}
