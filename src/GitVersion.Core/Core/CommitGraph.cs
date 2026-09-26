using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion;

/// <summary>
/// An immutable, in-memory projection of the commits reachable from a single head commit.
/// </summary>
/// <remarks>
/// <para>
/// Building the graph costs exactly one revision walk. Once built, the commit log between an arbitrary
/// base commit (exclusive) and the head commit (inclusive) is produced without performing another one. A base
/// commit which the head cannot reach is the one exception: its own ancestry is followed through
/// <see cref="ICommit.Parents"/> until the traversal re-enters the graph. This turns the repeated full revision
/// walks performed while evaluating every version tag into cheap in-memory set operations.
/// </para>
/// <para>
/// The commits are stored in the exact order produced by a <c>Topological | Time</c> walk. Removing the
/// ancestors of a base commit from that sequence selects exactly the commits a walk which hides the base
/// commit returns: the commits reachable from the head but not from the base are closed under taking
/// children, so no child of a retained commit is removed.
/// </para>
/// <para>
/// The resulting <em>order</em> is a valid topological order and matches the walk whenever the committer
/// timestamps of the selected commits are distinct. Commits which share a timestamp may be ordered
/// differently, because the walk breaks such ties through the shape of its priority queue, which changes
/// when commits are hidden. Callers which depend on the relative order of equally timed commits therefore
/// have to use the plain walk instead.
/// </para>
/// <para>
/// The graph deliberately covers the unfiltered history. Ignored commits have to stay part of it, because
/// dropping them would sever the parent links through which the ancestors of a base commit are reached.
/// They are left out of the produced commit log instead, which is where the revision walk dropped them too.
/// </para>
/// </remarks>
internal sealed class CommitGraph
{
    private readonly ICommit[] commits;
    private readonly Dictionary<string, int> indexBySha;
    private readonly int[][] parentIndexes;

    /// <summary>Use <see cref="Create"/>; the graph owns the arrays it is constructed with.</summary>
    private CommitGraph(ICommit[] commits, Dictionary<string, int> indexBySha, int[][] parentIndexes)
    {
        this.commits = commits;
        this.indexBySha = indexBySha;
        this.parentIndexes = parentIndexes;
    }

    /// <summary>The commits reachable from the head commit, ordered exactly as the revision walk emitted them.</summary>
    public IReadOnlyList<ICommit> Commits => this.commits;

    /// <summary>Creates the graph from the commits a revision walk emitted, in that exact order.</summary>
    /// <param name="commitsInWalkOrder">
    /// All commits reachable from the head commit, ordered as the walk produced them. The sequence has to be
    /// unfiltered, because the parent links of the commits it contains are resolved against it.
    /// </param>
    public static CommitGraph Create(IReadOnlyList<ICommit> commitsInWalkOrder)
    {
        commitsInWalkOrder.NotNull();

        var commits = commitsInWalkOrder as ICommit[] ?? [.. commitsInWalkOrder];

        var indexBySha = new Dictionary<string, int>(commits.Length, StringComparer.Ordinal);
        for (var index = 0; index < commits.Length; index++)
        {
            indexBySha[commits[index].Sha] = index;
        }

        var parentIndexes = new int[commits.Length][];
        for (var index = 0; index < commits.Length; index++)
        {
            var parents = commits[index].Parents;
            if (parents.Count == 0)
            {
                parentIndexes[index] = [];
                continue;
            }

            var resolved = new List<int>(parents.Count);
            foreach (var parent in parents)
            {
                // A parent outside the graph is unreachable from the head commit and can therefore never
                // appear in a commit log derived from this graph.
                if (indexBySha.TryGetValue(parent.Sha, out var parentIndex))
                {
                    resolved.Add(parentIndex);
                }
            }

            parentIndexes[index] = [.. resolved];
        }

        return new(commits, indexBySha, parentIndexes);
    }

    /// <summary>Builds the subset of the commits of this graph which <paramref name="ignore"/> filters out.</summary>
    public CommitMask CreateIgnoredMask(IIgnoreConfiguration ignore)
    {
        ignore.NotNull();

        var flags = new bool[this.commits.Length];
        Array.Fill(flags, true);

        var retainedCount = 0;
        foreach (var retained in ignore.Filter(this.commits))
        {
            flags[this.indexBySha[retained.Sha]] = false;
            retainedCount++;
        }

        return new(flags, this.commits.Length - retainedCount);
    }

    /// <summary>
    /// Builds the subset of the commits which are, or are an ancestor of, one of the commits identified by
    /// <paramref name="shas"/>. Commits which <paramref name="barrier"/> contains are neither part of the result
    /// nor traversed, so the walk stops at them.
    /// </summary>
    public CommitMask CreateAncestorMask(IEnumerable<string> shas, CommitMask barrier)
    {
        shas.NotNull();

        var flags = new bool[this.commits.Length];
        var pending = new Stack<int>();
        var marked = 0;

        foreach (var sha in shas)
        {
            if (this.indexBySha.TryGetValue(sha, out var index) && !flags[index] && !barrier.Contains(index))
            {
                flags[index] = true;
                marked++;
                pending.Push(index);
            }
        }

        marked += MarkParents(pending, flags, barrier);

        return new(flags, marked);
    }

    /// <summary>
    /// Returns the commits of this graph in revision walk order, leaving out <paramref name="baseVersionSource"/>
    /// together with all of its ancestors, as well as every commit which <paramref name="ignored"/> or
    /// <paramref name="excluded"/> contains.
    /// </summary>
    public IReadOnlyList<ICommit> GetCommits(ICommit? baseVersionSource, CommitMask ignored, CommitMask excluded)
    {
        var baseAncestors = baseVersionSource is null ? CommitMask.Empty : CreateAncestorsOfMask(baseVersionSource);

        if (baseAncestors.Count == 0 && ignored.Count == 0 && excluded.Count == 0)
        {
            return this.commits;
        }

        var result = new List<ICommit>(this.commits.Length - baseAncestors.Count);
        for (var index = 0; index < this.commits.Length; index++)
        {
            if (ignored.Contains(index) || baseAncestors.Contains(index) || excluded.Contains(index))
            {
                continue;
            }

            result.Add(this.commits[index]);
        }

        return result;
    }

    /// <summary>
    /// Builds the subset holding <paramref name="commit"/> and all of its ancestors which belong to this graph.
    /// Ignored commits are traversed as well, mirroring a revision walk which hides the commit and everything it
    /// builds upon.
    /// </summary>
    private CommitMask CreateAncestorsOfMask(ICommit commit)
    {
        var flags = new bool[this.commits.Length];
        var pending = new Stack<int>();
        var marked = 0;

        if (this.indexBySha.TryGetValue(commit.Sha, out var index))
        {
            flags[index] = true;
            marked++;
            pending.Push(index);
        }
        else
        {
            marked += SeedFromCommitOutsideTheGraph(commit, flags, pending);
        }

        marked += MarkParents(pending, flags, CommitMask.Empty);

        return new(flags, marked);
    }

    /// <summary>
    /// Marks the ancestors of a commit which is itself not reachable from the head commit, by walking the real
    /// commit graph until the traversal re-enters this graph. Every commit it re-enters at is pushed onto
    /// <paramref name="pending"/>, from where the cheap index based walk takes over, because every ancestor of a
    /// reachable commit is reachable as well. Returns how many commits it marked.
    /// </summary>
    private int SeedFromCommitOutsideTheGraph(ICommit commit, bool[] flags, Stack<int> pending)
    {
        var marked = 0;
        var visited = new HashSet<string>(StringComparer.Ordinal) { commit.Sha };
        var outside = new Stack<ICommit>();
        outside.Push(commit);

        while (outside.Count > 0)
        {
            foreach (var parent in outside.Pop().Parents)
            {
                if (!this.indexBySha.TryGetValue(parent.Sha, out var parentIndex))
                {
                    if (visited.Add(parent.Sha))
                    {
                        outside.Push(parent);
                    }
                }
                else if (!flags[parentIndex])
                {
                    flags[parentIndex] = true;
                    marked++;
                    pending.Push(parentIndex);
                }
            }
        }

        return marked;
    }

    /// <summary>
    /// Marks everything reachable from <paramref name="pending"/> through parent links, stopping at the commits
    /// <paramref name="barrier"/> contains, and returns how many commits it marked.
    /// </summary>
    private int MarkParents(Stack<int> pending, bool[] flags, CommitMask barrier)
    {
        var marked = 0;

        while (pending.Count > 0)
        {
            foreach (var parentIndex in this.parentIndexes[pending.Pop()])
            {
                if (flags[parentIndex] || barrier.Contains(parentIndex))
                {
                    continue;
                }

                flags[parentIndex] = true;
                marked++;
                pending.Push(parentIndex);
            }
        }

        return marked;
    }
}
