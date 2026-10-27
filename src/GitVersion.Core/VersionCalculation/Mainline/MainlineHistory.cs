using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

// Selects calculation history without changing the recorded iteration graph.
internal sealed class MainlineHistory(
    IGitVersionConfiguration configuration, IEnvironment environment, ICommit currentCommit,
    MainlineBranchPointResolver? branchPointResolver = null)
{
    private MainlineBranchPointResolver? branchPointResolver = branchPointResolver;

    private readonly Dictionary<ICommit, ICommit[]> unignoredCache = [];

    internal sealed record Selection(MainlineCommit[] Commits, bool HasBoundary, ICommit? MissingCommit = null);

    internal Selection Select(MainlineIteration iteration, string? targetLabel, bool includeAncestry,
        MainlineCommit? tip = null, bool includeSharedHistory = false)
    {
        this.branchPointResolver ??= new MainlineBranchPointResolver(iteration);
        List<MainlineCommit> commits = [];
        if (SelectLocal(iteration, targetLabel, includeAncestry, tip, commits, includeSharedHistory) is { } selection)
        {
            return selection;
        }

        // Git-based selection has already traversed its requested ancestry.
        if (UsesBranchHistory(iteration, includeSharedHistory) || !includeAncestry || iteration.BranchedFrom is null)
        {
            return Result(commits, false);
        }
        if (this.branchPointResolver.Resolve(iteration) is not { } branchPoint)
        {
            return Result(commits, false, iteration.BranchedFrom);
        }

        var ancestry = Select(branchPoint.Iteration, targetLabel, true, branchPoint,
            includeSharedHistory: includeSharedHistory);
        commits.AddRange(ancestry.Commits.Reverse());
        return Result(commits, ancestry.HasBoundary, ancestry.MissingCommit);
    }

    private Selection? SelectLocal(MainlineIteration iteration, string? targetLabel, bool includeAncestry,
        MainlineCommit? tip, List<MainlineCommit> commits, bool includeSharedHistory)
    {
        var foundTag = false;
        foreach (var (recorded, missing) in GetCommits(iteration, targetLabel, includeAncestry, tip, includeSharedHistory))
        {
            if (recorded is not { } commit)
            {
                return Result(commits, false, missing);
            }
            commits.Add(commit);
            var label = targetLabel ?? commit.GetBranchSpecificLabel(configuration, environment, currentCommit);
            foundTag |= commit.SemanticVersions.Any(version => version.IsMatchForBranchSpecificLabel(label));
            if (foundTag && commit.HasExplicitIncrement)
            {
                return Result(commits, true);
            }
            if (SelectChild(commit, targetLabel, includeSharedHistory) is { } child)
            {
                return Result(commits, child.HasBoundary, child.MissingCommit);
            }
        }
        return null;
    }

    private Selection? SelectChild(MainlineCommit commit, string? targetLabel, bool includeSharedHistory)
    {
        if (commit.ChildIteration is not { } child
            || commit.IsMergedMainline(configuration))
        {
            return null;
        }
        var selection = Select(child, targetLabel, false, includeSharedHistory: includeSharedHistory);
        return selection.HasBoundary || selection.MissingCommit is not null ? selection : null;
    }

    private static Selection Result(List<MainlineCommit> commits, bool hasBoundary, ICommit? missing = null)
        => new([.. commits.AsEnumerable().Reverse()], hasBoundary, missing);

    private IEnumerable<(MainlineCommit? Commit, ICommit? Missing)> GetCommits(
        MainlineIteration iteration, string? targetLabel, bool includeAncestry, MainlineCommit? tip, bool includeSharedHistory)
    {
        if (UsesBranchHistory(iteration, includeSharedHistory))
        {
            return GetMainlineCommits(iteration, targetLabel, includeAncestry, tip);
        }
        return GetIterationCommits(iteration, targetLabel, tip, includeSharedHistory);
    }

    private IEnumerable<(MainlineCommit? Commit, ICommit? Missing)> GetIterationCommits(
        MainlineIteration iteration, string? targetLabel, MainlineCommit? tip, bool includeSharedHistory)
    {
        foreach (var commit in iteration.GetCommitsInReverseOrder(false, tip))
        {
            if (configuration.Ignore.IsEmpty || configuration.Ignore.Filter([commit.Value]).Any())
            {
                yield return (commit, null);
            }
            else if (commit.ChildIteration is { } child)
            {
                // Ignoring the merge must not also discard its source commits.
                foreach (var source in GetCommits(child, targetLabel, false, null, includeSharedHistory))
                {
                    yield return source;
                }
            }
        }
    }

    private static bool UsesBranchHistory(MainlineIteration iteration, bool includeSharedHistory)
        => iteration.Configuration.IsMainBranch == true
            || includeSharedHistory && iteration is { ParentIteration: not null, BranchedFrom: not null };

    private IEnumerable<(MainlineCommit? Commit, ICommit? Missing)> GetMainlineCommits(
        MainlineIteration iteration, string? targetLabel, bool includeAncestry, MainlineCommit? tip)
    {
        var nodes = GetBranchHistory(iteration, targetLabel, includeAncestry, tip);
        foreach (var current in InReverseTopologicalOrder(nodes))
        {
            var (recorded, parents) = nodes[current];
            yield return (recorded, recorded is null ? current : null);
            if (recorded is null)
            {
                continue;
            }
            foreach (var transition in GetTransitions(recorded, parents.FirstOrDefault()))
            {
                yield return (transition, null);
            }
        }
    }

    private Dictionary<ICommit, (MainlineCommit? Commit, ICommit[] Parents)> GetBranchHistory(
        MainlineIteration iteration, string? targetLabel, bool includeAncestry, MainlineCommit? tip)
    {
        // Recording ancestry beyond a release can put side commits in an enclosing
        // iteration. Follow Git ancestry while reusing the existing recorded nodes.
        var boundary = GetUnignored(iteration.BranchedFrom).ToHashSet();
        var nodes = new Dictionary<ICommit, (MainlineCommit? Commit, ICommit[] Parents)>();
        Stack<ICommit> pending = new(GetUnignored(GetTip(iteration, includeAncestry, tip)));
        while (pending.TryPop(out var current))
        {
            if (nodes.ContainsKey(current) || (!includeAncestry && boundary.Contains(current)))
            {
                continue;
            }
            var recorded = this.branchPointResolver!.FindCommit(iteration, current);
            // Recognized sources have their own calculation. Untracked merges
            // retain their flattened history, including both parents.
            ICommit[] parents = [];
            if (recorded is not null && !HasRelease(recorded, targetLabel))
            {
                var branchParents = recorded.ChildIteration is null ? current.Parents : current.Parents.Take(1);
                parents = [.. branchParents
                    .SelectMany(GetUnignored)
                    .Where(parent => includeAncestry || !boundary.Contains(parent)).Distinct()];
            }
            nodes.Add(current, (recorded, parents));
            foreach (var parent in parents)
            {
                pending.Push(parent);
            }
        }
        return nodes;
    }

    private static IEnumerable<ICommit> InReverseTopologicalOrder(
        Dictionary<ICommit, (MainlineCommit? Commit, ICommit[] Parents)> nodes)
    {
        var children = nodes.Keys.ToDictionary(commit => commit, _ => 0);
        foreach (var parent in nodes.Values.SelectMany(node => node.Parents))
        {
            children[parent]++;
        }
        PriorityQueue<ICommit, long> ready = new();
        foreach (var (commit, count) in children)
        {
            if (count == 0)
            {
                ready.Enqueue(commit, -commit.When.UtcTicks);
            }
        }
        while (ready.TryDequeue(out var current, out _))
        {
            yield return current;
            foreach (var parent in nodes[current].Parents)
            {
                if (--children[parent] == 0)
                {
                    ready.Enqueue(parent, -parent.When.UtcTicks);
                }
            }
        }
    }

    private bool HasRelease(MainlineCommit commit, string? targetLabel)
    {
        var label = targetLabel ?? commit.GetBranchSpecificLabel(configuration, environment, currentCommit);
        return commit.HasExplicitIncrement
            && commit.SemanticVersions.Any(version => version.IsMatchForBranchSpecificLabel(label));
    }

    private static ICommit? GetTip(MainlineIteration iteration, bool includeAncestry, MainlineCommit? tip)
    {
        if (tip is not null)
        {
            return tip.Value;
        }
        if (iteration.ParentIteration is not null && iteration.BranchedTo is { } merge && merge.Parents.Count >= 2)
        {
            return merge.Parents[iteration.Configuration.IsMainBranch != true
                && iteration.BranchName == iteration.ParentIteration.BranchName ? 0 : 1];
        }
        return iteration.Commits.LastOrDefault()?.Value ?? (includeAncestry ? iteration.BranchedFrom : null);
    }

    private IEnumerable<ICommit> GetUnignored(ICommit? commit)
    {
        if (commit is null)
        {
            return [];
        }
        if (!this.unignoredCache.TryGetValue(commit, out var result))
        {
            result = [.. FindUnignored(commit)];
            this.unignoredCache.Add(commit, result);
        }
        return result;
    }

    private IEnumerable<ICommit> FindUnignored(ICommit commit)
    {
        Stack<ICommit> pending = new([commit]);
        HashSet<ICommit> visited = [];
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }
            if (configuration.Ignore.Filter([current]).Any())
            {
                yield return current;
                continue;
            }
            foreach (var parent in current.Parents)
            {
                pending.Push(parent);
            }
        }
    }

    private static IReadOnlyList<MainlineCommit> GetTransitions(MainlineCommit commit, ICommit? parent)
    {
        List<MainlineCommit> transitions = [];
        var predecessor = commit.Predecessor;
        while (predecessor is { IsDummy: true })
        {
            transitions.Add(predecessor);
            predecessor = predecessor.Predecessor;
        }
        return predecessor?.Value.Equals(parent) == true ? transitions : [];
    }
}
