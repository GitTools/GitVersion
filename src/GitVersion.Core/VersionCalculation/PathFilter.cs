using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal class PathFilter(IGitRepository repository, GitVersionContext context, IEnumerable<string> paths) : IVersionFilter
{
    public enum PathFilterMode { Inclusive = 0, Exclusive = 1 }

    private readonly IEnumerable<string> paths = paths.NotNull();
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
        reason = null;

        if (commit != null)
        {
            var patchPaths = repository.FindPatchPaths(commit, this.context.Configuration.TagPrefixPattern);

            if (patchPaths != null)
            {
                if (this.paths.Any(path => patchPaths.All(p => Regex.IsMatch(p, path, RegexOptions.IgnoreCase))))
                {
                    reason = "Source was ignored due to commit path matching ignore regex";
                    return true;
                }
            }
        }

        return false;
    }
}
