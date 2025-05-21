using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal class PathFilter( IEnumerable<string> paths) : IVersionFilter
{
    private readonly List<Regex> pathsRegexes = paths.Select(path => new Regex(path, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToList();
    private readonly Dictionary<string, bool> pathMatchCache = [];

    public bool Exclude(IBaseVersion baseVersion, [NotNullWhen(true)] out string? reason)
    {
        ArgumentNullException.ThrowIfNull(baseVersion);

        reason = null;
        if (baseVersion.Source.StartsWith("Fallback") || baseVersion.Source.StartsWith("Git tag") || baseVersion.Source.StartsWith("NextVersion")) return false;

        return Exclude(baseVersion.BaseVersionSource, out reason);
    }

    public bool Exclude(ICommit? commit, [NotNullWhen(true)] out string? reason)
    {
        reason = null;

        if (commit != null)
        {
            var patchPaths = commit.DiffPaths;

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
