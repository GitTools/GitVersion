namespace GitVersion.Git;

/// <summary>
/// Walks commit history in the orders GitVersion depends on (chronological, topological,
/// reversed, first-parent-only, with include/exclude reachability sets) and computes merge
/// bases. The walk replicates libgit2's revision-walk behavior — including its date-directed
/// limiting pass and its tie-breaking on equal committer timestamps — because GitVersion's
/// version output depends on the exact commit ordering. Parity is validated against libgit2
/// by the test suite.
/// </summary>
/// <remarks>
/// Missing parent objects (e.g. beyond a shallow-clone boundary) terminate traversal at
/// that point instead of failing the walk.
/// </remarks>
internal sealed class GitRevisionWalker(GitObjectStore objectStore)
{
    // The number of uninteresting commits to look at after running out of interesting ones,
    // matching git's slop heuristic for clock-skewed histories.
    private const int Slop = 5;

    private readonly GitObjectStore objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));
    private readonly Dictionary<GitObjectId, GitCommit?> commitCache = [];

    /// <summary>
    /// Walks the commits described by <paramref name="options"/> and returns them in order.
    /// </summary>
    /// <param name="options">The walk description.</param>
    /// <returns>The commits, in the requested order.</returns>
    public IReadOnlyList<GitCommit> Walk(GitRevisionWalkOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var walk = new WalkState(this, options);
        var result = walk.Run();

        if (options.Sort.HasFlag(GitRevisionSortStrategies.Reverse))
        {
            result.Reverse();
        }

        return [.. result.Select(node => node.Commit)];
    }

    /// <summary>
    /// Finds the best common ancestor of two commits, the way <c>git merge-base</c> does:
    /// paint-down-to-common followed by elimination of redundant candidates.
    /// </summary>
    /// <param name="first">The first commit.</param>
    /// <param name="second">The second commit.</param>
    /// <returns>The merge base, or <see langword="null"/> when the histories are unrelated.</returns>
    public GitObjectId? FindMergeBase(GitObjectId first, GitObjectId second)
    {
        if (first == second)
        {
            return TryLoad(first) is null ? null : first;
        }

        var candidates = PaintDownToCommon(first, second);

        return candidates.Count switch
        {
            0 => null,
            1 => candidates[0],
            _ => RemoveRedundantCandidates(candidates)
        };
    }

    private HashSet<GitObjectId> CollectReachable(IEnumerable<GitObjectId> roots, bool firstParentOnly)
    {
        var reachable = new HashSet<GitObjectId>();
        var stack = new Stack<GitObjectId>();

        foreach (var root in roots)
        {
            stack.Push(root);
        }

        while (stack.Count > 0)
        {
            var id = stack.Pop();

            if (!reachable.Add(id) || TryLoad(id) is not { } commit)
            {
                continue;
            }

            foreach (var parentId in firstParentOnly ? commit.Parents.Take(1) : commit.Parents)
            {
                stack.Push(parentId);
            }
        }

        return reachable;
    }

    private List<GitObjectId> PaintDownToCommon(GitObjectId first, GitObjectId second)
    {
        const int flagFirst = 1;
        const int flagSecond = 2;
        const int flagStale = 4;
        const int flagBoth = flagFirst | flagSecond;

        var flags = new Dictionary<GitObjectId, int>();
        var queuedCounts = new Dictionary<GitObjectId, int>();
        var candidates = new List<GitObjectId>();
        var queue = new PriorityQueue<GitCommit, (long Time, long Sequence)>(TimeDescendingComparer.Instance);
        var sequence = 0L;

        // The number of queued entries whose commit is not (yet) stale, tracked
        // incrementally: libgit2 rescans its whole queue per iteration to decide
        // whether anything interesting remains, which is quadratic on the hottest
        // path of version calculation. The count is equivalent to that scan.
        var interesting = 0;

        void MarkQueuedEntriesStale(GitObjectId id) => interesting -= queuedCounts.GetValueOrDefault(id);

        void Enqueue(GitObjectId id, int newFlags)
        {
            var current = flags.GetValueOrDefault(id);
            var updated = current | newFlags;

            if (updated == current)
            {
                return;
            }

            flags[id] = updated;

            if ((current & flagStale) == 0 && (updated & flagStale) != 0)
            {
                MarkQueuedEntriesStale(id);
            }

            if (TryLoad(id) is { } commit)
            {
                queue.Enqueue(commit, (commit.CommitterWhen.ToUnixTimeSeconds(), sequence++));
                queuedCounts[id] = queuedCounts.GetValueOrDefault(id) + 1;

                if ((updated & flagStale) == 0)
                {
                    interesting++;
                }
            }
        }

        Enqueue(first, flagFirst);
        Enqueue(second, flagSecond);

        while (interesting > 0)
        {
            var commit = queue.Dequeue();
            var commitFlags = flags[commit.Sha];
            queuedCounts[commit.Sha]--;

            if ((commitFlags & flagStale) == 0)
            {
                interesting--;
            }

            var paint = commitFlags & flagBoth;

            if (paint == flagBoth)
            {
                if ((commitFlags & flagStale) == 0)
                {
                    candidates.Add(commit.Sha);
                    flags[commit.Sha] = commitFlags | flagStale;
                    MarkQueuedEntriesStale(commit.Sha);
                }

                // Everything below a common commit is stale: it cannot be a better candidate.
                paint |= flagStale;
            }

            foreach (var parentId in commit.Parents)
            {
                Enqueue(parentId, paint);
            }
        }

        return candidates;
    }

    private GitObjectId? RemoveRedundantCandidates(List<GitObjectId> candidates)
    {
        // A candidate which is an ancestor of another candidate is redundant. Candidates are
        // in discovery order (most recent first), so return the first independent one.
        foreach (var candidate in candidates)
        {
            var others = candidates.Where(other => other != candidate).ToList();

            if (!IsAncestorOfAny(candidate, others))
            {
                return candidate;
            }
        }

        return candidates[0];
    }

    private bool IsAncestorOfAny(GitObjectId candidate, List<GitObjectId> others)
    {
        foreach (var other in others)
        {
            if (TryLoad(other) is not { } commit)
            {
                continue;
            }

            if (CollectReachable(commit.Parents, firstParentOnly: false).Contains(candidate))
            {
                return true;
            }
        }

        return false;
    }

    private GitCommit? TryLoad(GitObjectId id)
    {
        if (this.commitCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        GitCommit? commit = null;

        if (this.objectStore.TryGetObject(id, GitObjectTypes.Commit, out var stream))
        {
            using (stream)
            {
                commit = GitCommitReader.Read(stream, id);
            }
        }

        this.commitCache[id] = commit;
        return commit;
    }

    /// <summary>
    /// The per-walk engine. Mirrors libgit2's revwalk phases: seed preparation, the
    /// date-directed limiting pass, and the per-sort emission disciplines.
    /// </summary>
    private sealed class WalkState(GitRevisionWalker walker, GitRevisionWalkOptions options)
    {
        private readonly Dictionary<GitObjectId, WalkNode> nodes = [];

        private bool FirstParentOnly => options.FirstParentOnly;

        public List<WalkNode> Run()
        {
            var (seeds, limited) = PrepareSeeds();

            if (limited)
            {
                seeds = LimitList(seeds);
            }

            if (options.Sort.HasFlag(GitRevisionSortStrategies.Topological))
            {
                return SortInTopologicalOrder(seeds, byTime: options.Sort.HasFlag(GitRevisionSortStrategies.Time));
            }

            if (options.Sort.HasFlag(GitRevisionSortStrategies.Time))
            {
                return EmitByTime(seeds);
            }

            return limited ? [.. seeds.Where(node => !node.Uninteresting)] : EmitUnsorted(seeds);
        }

        private (List<WalkNode> Seeds, bool Limited) PrepareSeeds()
        {
            var limited = options.Sort != GitRevisionSortStrategies.None;

            // Pushes prepend to the input list; includes are pushed before hides,
            // and a hide overrides an earlier push of the same commit.
            var userInput = new List<WalkNode>();

            foreach (var (id, uninteresting) in
                     options.Include.Select(id => (id, false)).Concat(options.Exclude.Select(id => (id, true))))
            {
                var node = LookupNode(id);

                if (node.Uninteresting && !uninteresting)
                {
                    continue;
                }

                node.Uninteresting = uninteresting;
                limited |= uninteresting;
                userInput.Insert(0, node);
            }

            var seeds = new List<WalkNode>();

            foreach (var node in userInput)
            {
                Parse(node);

                // libgit2's revwalk push/hide fails when the commit cannot be looked up;
                // flowing an unparsed seed through the walk would emit a null commit.
                if (!node.Parsed)
                {
                    throw new GitObjectStoreException($"The commit '{node.Id}' could not be found in the repository.") { ObjectNotFound = true };
                }

                if (node.Uninteresting)
                {
                    MarkParentsUninteresting(node);
                }

                if (!node.Seen)
                {
                    node.Seen = true;
                    seeds.Add(node);
                }
            }

            return (seeds, limited);
        }

        /// <summary>
        /// The date-directed limiting pass: traverses from the seeds in descending date order,
        /// propagating "uninteresting" and stopping a few commits (the slop) after only
        /// uninteresting commits remain. Returns the interesting commits in traversal order.
        /// </summary>
        private List<WalkNode> LimitList(List<WalkNode> seeds)
        {
            var list = new LinkedList<WalkNode>(seeds);
            var newList = new List<WalkNode>();
            var slop = Slop;
            var time = long.MaxValue;

            while (list.First is { } head)
            {
                list.RemoveFirst();
                var commit = head.Value;

                AddParentsToList(commit, list);

                if (commit.Uninteresting)
                {
                    MarkParentsUninteresting(commit);

                    slop = StillInteresting(list, time, slop);

                    if (slop > 0)
                    {
                        continue;
                    }

                    break;
                }

                time = commit.Time;
                newList.Add(commit);
            }

            return newList;
        }

        private void AddParentsToList(WalkNode commit, LinkedList<WalkNode> list)
        {
            if (commit.Added)
            {
                return;
            }

            commit.Added = true;

            if (commit.Uninteresting)
            {
                // Hidden histories ignore first-parent simplification: hide as much as possible.
                foreach (var parent in ParsedParents(commit))
                {
                    parent.Uninteresting = true;
                    Parse(parent);

                    if (parent.Parents.Count > 0)
                    {
                        MarkParentsUninteresting(parent);
                    }

                    parent.Seen = true;
                    InsertByDate(list, parent);
                }

                return;
            }

            foreach (var parent in ParsedParents(commit))
            {
                Parse(parent);

                if (parent.Parsed && !parent.Seen)
                {
                    parent.Seen = true;
                    InsertByDate(list, parent);
                }

                if (FirstParentOnly)
                {
                    break;
                }
            }
        }

        private static int StillInteresting(LinkedList<WalkNode> list, long time, int slop)
        {
            if (list.Count == 0)
            {
                return 0;
            }

            // A destination commit later than our current date means we are not done.
            if (time <= list.First!.Value.Time)
            {
                return Slop;
            }

            foreach (var item in list)
            {
                if (!item.Uninteresting || item.Time > time)
                {
                    return Slop;
                }
            }

            return slop - 1;
        }

        private static void InsertByDate(LinkedList<WalkNode> list, WalkNode item)
        {
            // Keep the list in descending date order; equal dates go after existing entries.
            var current = list.First;

            while (current is not null && current.Value.Time >= item.Time)
            {
                current = current.Next;
            }

            if (current is null)
            {
                list.AddLast(item);
            }
            else
            {
                list.AddBefore(current, item);
            }
        }

        private static void MarkParentsUninteresting(WalkNode node)
        {
            // Mark all (currently parsed) ancestors uninteresting, chasing first-parent
            // chains and stopping at commits which have not been parsed yet — the limiting
            // pass will pick those up as it reaches them.
            var pending = new Stack<WalkNode>();

            foreach (var parent in ParsedParents(node))
            {
                pending.Push(parent);
            }

            while (pending.Count > 0)
            {
                var commit = pending.Pop();

                while (true)
                {
                    if (commit.Uninteresting)
                    {
                        break;
                    }

                    commit.Uninteresting = true;

                    if (!commit.Parsed || commit.Parents.Count == 0)
                    {
                        break;
                    }

                    foreach (var parent in commit.Parents)
                    {
                        pending.Push(parent);
                    }

                    commit = commit.Parents[0];
                }
            }
        }

        private static List<WalkNode> EmitByTime(List<WalkNode> seeds)
        {
            var queue = new BinaryHeap(byTime: true);

            foreach (var node in seeds)
            {
                queue.Insert(node);
            }

            var result = new List<WalkNode>();

            while (queue.Pop() is { } next)
            {
                if (!next.Uninteresting)
                {
                    result.Add(next);
                }
            }

            return result;
        }

        private List<WalkNode> EmitUnsorted(List<WalkNode> seeds)
        {
            // The default walk: pop the head of a date-sorted list, lazily inserting parents.
            var list = new LinkedList<WalkNode>(seeds);
            var result = new List<WalkNode>();

            while (list.First is { } head)
            {
                list.RemoveFirst();
                var commit = head.Value;

                AddParentsToList(commit, list);

                if (!commit.Uninteresting)
                {
                    result.Add(commit);
                }
            }

            return result;
        }

        private static List<WalkNode> SortInTopologicalOrder(List<WalkNode> list, bool byTime)
        {
            // A commit may only be emitted after all commits in the list which have it as a
            // parent. Ready commits are emitted chronologically when time-sorting, otherwise
            // in the order the limiting pass produced them.
            ComputeChildCounts(list);

            var queue = CreateReadyQueue(list, byTime);
            var result = new List<WalkNode>();

            while (queue.Pop() is { } next)
            {
                foreach (var parent in next.Parents.Where(parent => parent.InDegree > 0))
                {
                    if (--parent.InDegree == 1)
                    {
                        queue.Insert(parent);
                    }
                }

                next.InDegree = 0;

                if (!next.Uninteresting)
                {
                    result.Add(next);
                }
            }

            return result;
        }

        private static void ComputeChildCounts(List<WalkNode> list)
        {
            foreach (var node in list)
            {
                node.InDegree = 1;
            }

            foreach (var parent in list.SelectMany(node => node.Parents).Where(parent => parent.InDegree > 0))
            {
                parent.InDegree++;
            }
        }

        private static BinaryHeap CreateReadyQueue(List<WalkNode> list, bool byTime)
        {
            var queue = new BinaryHeap(byTime);

            foreach (var node in list.Where(node => node.InDegree == 1))
            {
                queue.Insert(node);
            }

            // The tips must come out in traversal order; without time-sorting the plain
            // vector pops from the end, so reverse it to preserve the insertion order.
            if (!byTime)
            {
                queue.Reverse();
            }

            return queue;
        }

        private static IEnumerable<WalkNode> ParsedParents(WalkNode node)
        {
            if (!node.Parsed)
            {
                yield break;
            }

            foreach (var parent in node.Parents)
            {
                yield return parent;
            }
        }

        private WalkNode LookupNode(GitObjectId id)
        {
            if (!this.nodes.TryGetValue(id, out var node))
            {
                node = new(id);
                this.nodes.Add(id, node);
            }

            return node;
        }

        private void Parse(WalkNode node)
        {
            if (node.Parsed || walker.TryLoad(node.Id) is not { } commit)
            {
                return;
            }

            node.Commit = commit;
            node.Time = commit.CommitterWhen.ToUnixTimeSeconds();
            node.Parents.AddRange(commit.Parents.Select(LookupNode));
            node.Parsed = true;
        }
    }

    private sealed class WalkNode(GitObjectId id)
    {
        public GitObjectId Id { get; } = id;
        public GitCommit Commit { get; set; } = null!;
        public long Time { get; set; }
        public List<WalkNode> Parents { get; } = [];
        public bool Parsed { get; set; }
        public bool Seen { get; set; }
        public bool Uninteresting { get; set; }
        public bool Added { get; set; }
        public int InDegree { get; set; }
    }

    /// <summary>
    /// A binary min-heap over descending commit time, replicating the classic array-heap
    /// mechanics (append + sift-up on insert; move-last-to-root + sift-down on pop, with
    /// strict comparisons) so that the emission order of equal-timestamp commits matches
    /// libgit2's. Without time ordering it degrades to a plain vector popped from the end.
    /// </summary>
    private sealed class BinaryHeap(bool byTime)
    {
        private readonly List<WalkNode> items = [];

        public void Insert(WalkNode item)
        {
            this.items.Add(item);

            if (byTime)
            {
                SiftUp(this.items.Count - 1);
            }
        }

        public void Reverse() => this.items.Reverse();

        public WalkNode? Pop()
        {
            if (this.items.Count == 0)
            {
                return null;
            }

            if (!byTime)
            {
                var last = this.items[^1];
                this.items.RemoveAt(this.items.Count - 1);
                return last;
            }

            var result = this.items[0];

            if (this.items.Count > 1)
            {
                this.items[0] = this.items[^1];
                this.items.RemoveAt(this.items.Count - 1);
                SiftDown(0);
            }
            else
            {
                this.items.RemoveAt(0);
            }

            return result;
        }

        private static int Compare(WalkNode left, WalkNode right) => right.Time.CompareTo(left.Time);

        private void SiftUp(int index)
        {
            var item = this.items[index];

            while (index > 0)
            {
                var parentIndex = (index - 1) >> 1;
                var parent = this.items[parentIndex];

                if (Compare(parent, item) <= 0)
                {
                    break;
                }

                this.items[index] = parent;
                index = parentIndex;
            }

            this.items[index] = item;
        }

        private void SiftDown(int index)
        {
            var item = this.items[index];

            while (true)
            {
                var childIndex = (index << 1) + 1;

                if (childIndex >= this.items.Count)
                {
                    break;
                }

                if (childIndex + 1 < this.items.Count && Compare(this.items[childIndex], this.items[childIndex + 1]) > 0)
                {
                    childIndex++;
                }

                if (Compare(item, this.items[childIndex]) <= 0)
                {
                    break;
                }

                this.items[index] = this.items[childIndex];
                index = childIndex;
            }

            this.items[index] = item;
        }
    }

    private sealed class TimeDescendingComparer : IComparer<(long Time, long Sequence)>
    {
        public static TimeDescendingComparer Instance { get; } = new();

        public int Compare((long Time, long Sequence) x, (long Time, long Sequence) y)
        {
            var byTime = y.Time.CompareTo(x.Time);
            return byTime != 0 ? byTime : x.Sequence.CompareTo(y.Sequence);
        }
    }
}
