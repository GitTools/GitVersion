namespace GitVersion.Git;

/// <summary>
/// Well-known Git object type names.
/// </summary>
internal static class GitObjectTypes
{
    public const string Commit = "commit";
    public const string Tree = "tree";
    public const string Blob = "blob";
    public const string Tag = "tag";

    /// <summary>
    /// Returns the canonical (interned) object type name for the given UTF-8 encoded type.
    /// </summary>
    /// <param name="type">The UTF-8 encoded object type name.</param>
    /// <returns>The canonical object type name.</returns>
    public static string Canonicalize(ReadOnlySpan<byte> type)
    {
        if (type.SequenceEqual("commit"u8))
        {
            return Commit;
        }

        if (type.SequenceEqual("tree"u8))
        {
            return Tree;
        }

        if (type.SequenceEqual("blob"u8))
        {
            return Blob;
        }

        if (type.SequenceEqual("tag"u8))
        {
            return Tag;
        }

        throw new GitObjectStoreException($"Unknown git object type '{Encoding.UTF8.GetString(type)}'.");
    }
}
