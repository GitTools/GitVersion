using System.Text.RegularExpressions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal class PathFilter(IGitRepository repository, GitVersionContext context, IEnumerable<string> paths) : IVersionFilter
{
    private readonly GitVersionContext context = context;
    private readonly List<Regex> pathsRegexes = paths.Select(path => new Regex(path, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToList();
    private readonly Dictionary<string, bool> pathMatchCache = [];

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
                foreach (var path in patchPaths)
                {
                    if (!pathMatchCache.TryGetValue(path, out var isMatch))
                    {
                        isMatch = this.pathsRegexes.Any(regex => regex.IsMatch(path));
                        pathMatchCache[path] = isMatch;
                    }

                    if (isMatch)
                    {
                        reason = "Source was ignored due to commit path matching ignore regex";
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
