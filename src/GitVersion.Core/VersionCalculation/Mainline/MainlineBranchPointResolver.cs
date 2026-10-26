using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

// Built from the completed graph; resolution never changes node ownership or links.
internal sealed class MainlineBranchPointResolver
{
    private readonly List<MainlineIteration> iterations = [];
    private readonly Dictionary<ReferenceName, List<MainlineIteration>> iterationsByBranch = [];

    public MainlineBranchPointResolver(MainlineIteration iteration)
    {
        while (iteration.ParentIteration is { } parent)
        {
            iteration = parent;
        }

        Stack<MainlineIteration> pending = new([iteration]);
        HashSet<MainlineIteration> visited = [];
        while (pending.TryPop(out var current))
        {
            if (!visited.Add(current))
            {
                continue;
            }
            this.iterations.Add(current);
            if (!this.iterationsByBranch.TryGetValue(current.BranchName, out var branchIterations))
            {
                branchIterations = [];
                this.iterationsByBranch.Add(current.BranchName, branchIterations);
            }
            branchIterations.Add(current);
            foreach (var commit in current.Commits)
            {
                if (commit.ChildIteration is { } child)
                {
                    pending.Push(child);
                }
            }
        }
    }

    public MainlineCommit? Resolve(MainlineIteration iteration)
        => iteration.BranchedFrom is { } commit ? FindCommit(iteration, commit) : null;

    public MainlineCommit? FindCommit(MainlineIteration iteration, ICommit commit)
    {
        if (iteration.FindCommit(commit) is { } local)
        {
            return local;
        }
        if (this.iterationsByBranch.TryGetValue(iteration.BranchName, out var branchIterations))
        {
            foreach (var candidate in branchIterations)
            {
                if (candidate.FindCommit(commit) is { } found)
                {
                    return found;
                }
            }
        }
        foreach (var candidate in this.iterations)
        {
            if (candidate.BranchName != iteration.BranchName && candidate.FindCommit(commit) is { } found)
            {
                return found;
            }
        }
        return null;
    }

    internal IEnumerable<MainlineCommit> GetHistory(MainlineIteration iteration, MainlineCommit? tip = null)
        => GetCommitsInReverseOrder(iteration, tip).Reverse();

    private IEnumerable<MainlineCommit> GetCommitsInReverseOrder(MainlineIteration iteration, MainlineCommit? tip)
    {
        foreach (var commit in iteration.GetCommitsInReverseOrder(false, tip))
        {
            yield return commit;
        }
        if (Resolve(iteration) is { } branchPoint)
        {
            foreach (var ancestor in GetCommitsInReverseOrder(branchPoint.Iteration, branchPoint))
            {
                yield return ancestor;
            }
        }
    }
}
