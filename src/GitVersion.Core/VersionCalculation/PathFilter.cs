using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation;

internal enum PathFilterMode
{
    Inclusive,  // All commit paths must match for commit to be excluded
    //Exclusive // Any commit path must match for commit to be excluded
}

internal class PathFilter(IReadOnlyList<string> paths, PathFilterMode mode = PathFilterMode.Inclusive) : IVersionFilter
{
    private readonly IReadOnlyList<Regex> pathsRegexes = [.. paths.Select(path => new Regex(path, RegexOptions.Compiled))];
    private readonly ConcurrentDictionary<string, bool> pathMatchCache = [];

    public bool Exclude(IBaseVersion baseVersion, [NotNullWhen(true)] out string? reason)
    {
        ArgumentNullException.ThrowIfNull(baseVersion);
        return Exclude(baseVersion.BaseVersionSource, out reason);
    }

    private bool IsMatch(string path)
    {
        if (!pathMatchCache.TryGetValue(path, out var isMatch))
        {
            isMatch = this.pathsRegexes.Any(regex => regex.IsMatch(path));
            pathMatchCache[path] = isMatch;
        }
        return isMatch;
    }

    public bool Exclude(ICommit? commit, [NotNullWhen(true)] out string? reason)
    {
        reason = null;
        if (commit == null)
        {
            return false;
        }

        switch (mode)
        {
            case PathFilterMode.Inclusive:
                {
                    if (commit.DiffPaths.All(this.IsMatch))
                    {
                        reason = "Source was ignored due to all commit paths matching ignore regex";
                        return true;
                    }
                    break;
                }
        }

        return false;
    }
}
