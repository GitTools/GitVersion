namespace GitVersion.Git;

/// <summary>
/// Computes the paths changed between two trees: a recursive tree-vs-tree diff with no
/// rename detection, matching libgit2's default <c>TreeChanges</c> path list (which is
/// what GitVersion's <c>ICommit.DiffPaths</c> exposes). Identical subtrees are skipped
/// by object id; entries align using git's canonical tree order, where directory names
/// compare as if they had a trailing slash.
/// </summary>
internal sealed class GitTreeDiff(GitObjectStore objectStore)
{
    private readonly GitObjectStore objectStore = objectStore ?? throw new ArgumentNullException(nameof(objectStore));

    /// <summary>
    /// Returns the repository-relative paths which differ between the two trees,
    /// in path order. Either side may be <see langword="null"/> to diff against the
    /// empty tree (e.g. for root commits).
    /// </summary>
    /// <param name="oldTreeId">The object id of the old tree, or <see langword="null"/> for the empty tree.</param>
    /// <param name="newTreeId">The object id of the new tree, or <see langword="null"/> for the empty tree.</param>
    /// <returns>The changed paths.</returns>
    public IReadOnlyList<string> GetChangedPaths(GitObjectId? oldTreeId, GitObjectId? newTreeId)
    {
        if (Nullable.Equals(oldTreeId, newTreeId))
        {
            return [];
        }

        var paths = new List<string>();
        DiffTrees(LoadTree(oldTreeId), LoadTree(newTreeId), prefix: "", paths);
        return paths;
    }

    /// <summary>
    /// Flattens a tree into all of its blob paths and their entries.
    /// </summary>
    /// <param name="treeId">The object id of the tree, or <see langword="null"/> for the empty tree.</param>
    /// <returns>A dictionary of repository-relative path to tree entry.</returns>
    public Dictionary<string, GitTreeEntry> FlattenTree(GitObjectId? treeId)
    {
        var files = new Dictionary<string, GitTreeEntry>(StringComparer.Ordinal);

        if (treeId is { } id)
        {
            Flatten(this.objectStore.GetTree(id), prefix: "", files);
        }

        return files;
    }

    private void Flatten(GitTree tree, string prefix, Dictionary<string, GitTreeEntry> files)
    {
        foreach (var entry in tree.Entries)
        {
            if (entry.IsTree)
            {
                Flatten(this.objectStore.GetTree(entry.Sha), prefix + entry.Name + "/", files);
            }
            else
            {
                files[prefix + entry.Name] = entry;
            }
        }
    }

    private GitTree? LoadTree(GitObjectId? treeId) => treeId is { } id ? this.objectStore.GetTree(id) : null;

    private void DiffTrees(GitTree? oldTree, GitTree? newTree, string prefix, List<string> paths)
    {
        var oldEntries = oldTree?.Entries ?? [];
        var newEntries = newTree?.Entries ?? [];
        var oldIndex = 0;
        var newIndex = 0;

        while (oldIndex < oldEntries.Count || newIndex < newEntries.Count)
        {
            var comparison = (oldIndex < oldEntries.Count, newIndex < newEntries.Count) switch
            {
                (true, false) => -1,
                (false, true) => 1,
                _ => CompareTreeNames(oldEntries[oldIndex], newEntries[newIndex])
            };

            if (comparison < 0)
            {
                EmitAll(oldEntries[oldIndex++], prefix, paths);
            }
            else if (comparison > 0)
            {
                EmitAll(newEntries[newIndex++], prefix, paths);
            }
            else
            {
                DiffAlignedEntries(oldEntries[oldIndex++], newEntries[newIndex++], prefix, paths);
            }
        }
    }

    private void DiffAlignedEntries(GitTreeEntry oldEntry, GitTreeEntry newEntry, string prefix, List<string> paths)
    {
        if (oldEntry.Sha == newEntry.Sha && oldEntry.Mode == newEntry.Mode)
        {
            return;
        }

        if (oldEntry.IsTree && newEntry.IsTree)
        {
            if (oldEntry.Sha != newEntry.Sha)
            {
                DiffTrees(LoadTree(oldEntry.Sha), LoadTree(newEntry.Sha), prefix + oldEntry.Name + "/", paths);
            }

            return;
        }

        if (!oldEntry.IsTree && !newEntry.IsTree)
        {
            paths.Add(prefix + oldEntry.Name);
            return;
        }

        // The name changed type between a file and a directory: the file path sorts
        // before the paths inside the directory.
        var (file, tree) = oldEntry.IsTree ? (newEntry, oldEntry) : (oldEntry, newEntry);
        paths.Add(prefix + file.Name);
        EmitAll(tree, prefix, paths);
    }

    private void EmitAll(GitTreeEntry entry, string prefix, List<string> paths)
    {
        if (!entry.IsTree)
        {
            paths.Add(prefix + entry.Name);
            return;
        }

        var tree = this.objectStore.GetTree(entry.Sha);

        foreach (var child in tree.Entries)
        {
            EmitAll(child, prefix + entry.Name + "/", paths);
        }
    }

    private static int CompareTreeNames(GitTreeEntry left, GitTreeEntry right)
    {
        // Git sorts tree entries by their raw unsigned bytes, not by the decoded
        // strings: UTF-16 comparison inverts e.g. U+E000..U+FFFF versus surrogate
        // pairs, and Latin-1-fallback names do not round-trip through the string.
        var leftName = left.NameBytes.Span;
        var rightName = right.NameBytes.Span;

        // Entries with the same name align even when their type differs, so a
        // file-to-directory change on one name is detected as such.
        if (leftName.SequenceEqual(rightName))
        {
            return 0;
        }

        var commonLength = Math.Min(leftName.Length, rightName.Length);
        var comparison = leftName[..commonLength].SequenceCompareTo(rightName[..commonLength]);

        if (comparison != 0)
        {
            return comparison;
        }

        // One name is a prefix of the other. Git's canonical tree order: directory
        // names sort as if they ended with '/', while an exhausted file name ends.
        return NextByte(leftName, commonLength, left.IsTree) - NextByte(rightName, commonLength, right.IsTree);
    }

    private static int NextByte(ReadOnlySpan<byte> name, int index, bool isTree)
    {
        if (index < name.Length)
        {
            return name[index];
        }

        return isTree ? (byte)'/' : 0;
    }
}
