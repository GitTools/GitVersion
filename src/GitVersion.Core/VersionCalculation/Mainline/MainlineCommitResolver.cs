using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

// Built from the completed graph; resolution never changes node ownership or links.
internal sealed class MainlineCommitResolver : IMainlineCommitResolver
{
    private readonly List<MainlineIteration> iterations = [];
    private readonly Dictionary<ReferenceName, List<MainlineIteration>> iterationsByBranch = [];

    public MainlineCommitResolver(MainlineIteration iteration)
    {
        iteration.NotNull();
        if (iteration.ParentIteration is not null)
        {
            throw new ArgumentException("The iteration must be the root iteration.", nameof(iteration));
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

    public MainlineCommit? Resolve(MainlineIteration iteration, ICommit? commit)
    {
        iteration.NotNull();
        if (commit is null)
        {
            return null;
        }
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

    // Follow local predecessors, then the receiving merge: a merged feature inherits
    // the receiver's settings, even when its original branch point belongs to another branch.
    public IEnumerable<MainlineCommit> Traverse(MainlineCommit commit)
    {
        MainlineCommit? current = commit.NotNull();
        do
        {
            yield return current;

            if (current.HasPredecessor)
            {
                current = current.Predecessor;
            }
            else if (current.Iteration.HasParentIteration)
            {
                current = Resolve(
                    iteration: current.Iteration,
                    commit: current.Iteration.BranchedTo
                );
            }
            else
            {
                current = null;
            }
        }
        while (current is not null);
    }

    public IEnumerable<MainlineCommit> Traverse(MainlineIteration iteration)
    {
        iteration.NotNull();

        MainlineCommit? commit = iteration.Commits.FirstOrDefault();
        if (commit is null && iteration.HasParentIteration)
        {
            commit = Resolve(iteration, iteration.BranchedFrom);
        }
        return commit is not null ? Traverse(commit) : [];
    }
}
