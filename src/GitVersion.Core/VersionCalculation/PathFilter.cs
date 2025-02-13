using GitVersion.Extensions;
using GitVersion.Git;
using LibGit2Sharp;

namespace GitVersion.VersionCalculation;

internal class PathFilter(IGitRepository repository, GitVersionContext context, IEnumerable<string> paths, PathFilter.PathFilterMode mode = PathFilter.PathFilterMode.Inclusive) : IVersionFilter
{
    private readonly static Dictionary<string, Patch> patchsCache = [];

    public enum PathFilterMode { Inclusive = 0, Exclusive = 1 }

    private readonly IEnumerable<string> paths = paths.NotNull();
    private readonly PathFilterMode mode = mode;
    private readonly GitVersionContext context = context;

    public bool Exclude(IBaseVersion baseVersion, out string? reason)
    {
        ArgumentNullException.ThrowIfNull(baseVersion);

        reason = null;
        if (baseVersion.Source.StartsWith("Fallback") || baseVersion.Source.StartsWith("Git tag") || baseVersion.Source.StartsWith("NextVersion")) return false;

        return Exclude(baseVersion.BaseVersionSource, out reason);
    }

    public bool Exclude(ICommit? localCommit, out string? reason)
    {
        localCommit.NotNull();
        var commit = repository.InnerCommits.First(c => c.Sha == localCommit.Sha);

        reason = null;

        var match = new System.Text.RegularExpressions.Regex($"^({context.Configuration.TagPrefixPattern}).*$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        if (commit != null)
        {
            Patch? patch = null;
            if (!patchsCache.ContainsKey(commit.Sha))
            {
                if (!repository.InnerTags.Any(t => t.Target.Sha == commit.Sha && match.IsMatch(t.FriendlyName)))
                {
                    Tree commitTree = commit.Tree; // Main Tree
                    Tree? parentCommitTree = commit.Parents.FirstOrDefault()?.Tree; // Secondary Tree
                    patch = repository.InnerDiff.Compare<Patch>(parentCommitTree, commitTree); // Difference
                }
                patchsCache[commit.Sha] = patch;
            }

            patch = patchsCache[commit.Sha];
            if (patch != null)
            {
                switch (mode)
                {
                    case PathFilterMode.Inclusive:
                        if (!paths.Any(path => patch.Any(p => p.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))))
                        {
                            reason = "Source was ignored due to commit path is not present";
                            return true;
                        }
                        break;
                    case PathFilterMode.Exclusive:
                        if (paths.Any(path => patch.All(p => p.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase))))
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
