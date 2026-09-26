using GitVersion.Extensions;

namespace GitVersion;

/// <summary>
/// A subset of the commits of a single <see cref="CommitGraph"/>, holding one flag per position in that graph.
/// </summary>
/// <remarks>
/// <para>
/// Producing a commit log asks, for every commit of the graph and for every candidate version tag, whether that
/// commit belongs to a handful of such subsets. The positions the graph assigns to its commits turn that question
/// into a single array read, whereas a set keyed on the commit SHA hashes a forty character string every time. On
/// the history which motivated this type that is of the order of ten million membership tests per version
/// calculation, which is why the representation is a flag per position rather than a <see cref="HashSet{T}"/> of
/// SHAs.
/// </para>
/// <para>
/// A mask is only meaningful together with the graph that produced it, because that graph assigns the positions.
/// The default value is the empty subset, which contains no commit at all.
/// </para>
/// </remarks>
internal readonly struct CommitMask
{
    private readonly bool[]? flags;

    /// <summary>Creates a mask over the positions of a single graph.</summary>
    /// <param name="flags">One flag per position of the owning graph; the mask takes ownership of the array.</param>
    /// <param name="count">
    /// How many of <paramref name="flags"/> are set. The builders know this as they mark, so it is passed in
    /// rather than recomputed by scanning the array.
    /// </param>
    public CommitMask(bool[] flags, int count)
    {
        this.flags = flags.NotNull();
        Count = count;
    }

    /// <summary>The subset which contains no commit at all.</summary>
    public static CommitMask Empty => default;

    /// <summary>The number of commits in this subset.</summary>
    public int Count { get; }

    /// <summary>Reports whether the commit at <paramref name="index"/> of the owning graph belongs to this subset.</summary>
    public bool Contains(int index) => this.flags is not null && this.flags[index];
}
