// Portions derived from Nerdbank.GitVersioning (https://github.com/dotnet/Nerdbank.GitVersioning), MIT License.

namespace GitVersion.Git;

/// <summary>
/// Represents a Git tree, as stored in the Git object database.
/// </summary>
internal sealed class GitTree
{
    /// <summary>
    /// Gets the <see cref="GitObjectId"/> of this tree.
    /// </summary>
    public required GitObjectId Sha { get; init; }

    /// <summary>
    /// Gets the entries in this tree, in the order in which they are stored.
    /// </summary>
    public required IReadOnlyList<GitTreeEntry> Entries { get; init; }

    /// <summary>
    /// Gets the entry with the given name, or <see langword="null"/> if no such entry exists.
    /// </summary>
    /// <param name="name">The name of the entry to find.</param>
    /// <returns>The matching entry, if found.</returns>
    public GitTreeEntry? FindEntry(string name) => Entries.FirstOrDefault(entry => entry.Name == name);

    /// <inheritdoc/>
    public override string ToString() => $"Git tree: {Sha}";
}

/// <summary>
/// Represents an individual entry in a Git tree.
/// </summary>
/// <param name="name">The name of the entry.</param>
/// <param name="mode">The file mode of the entry, as stored in the tree object (octal, without leading zeros), e.g. <c>100644</c> or <c>40000</c>.</param>
/// <param name="sha">The Git object id of the blob or tree of the entry.</param>
internal sealed class GitTreeEntry(string name, string mode, GitObjectId sha)
{
    private const string TreeMode = "40000";

    /// <summary>
    /// Gets the name of the entry.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the file mode of the entry, as stored in the tree object (octal, without leading zeros),
    /// e.g. <c>100644</c> for a regular file or <c>40000</c> for a directory.
    /// </summary>
    public string Mode { get; } = mode;

    /// <summary>
    /// Gets the Git object id of the blob or tree of the entry.
    /// </summary>
    public GitObjectId Sha { get; } = sha;

    /// <summary>
    /// Gets a value indicating whether this entry points to a tree (directory).
    /// </summary>
    public bool IsTree => Mode == TreeMode;

    /// <inheritdoc/>
    public override string ToString() => Name;
}
