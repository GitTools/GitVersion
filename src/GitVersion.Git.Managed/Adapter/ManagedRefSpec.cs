using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class ManagedRefSpec : IRefSpec
{
    private static readonly LambdaEqualityHelper<IRefSpec> equalityHelper = new(x => x.Specification);
    private static readonly LambdaKeyComparer<IRefSpec, string> comparerHelper = new(x => x.Specification);

    internal ManagedRefSpec(string specification, RefSpecDirection direction)
    {
        Specification = specification.NotNull();
        Direction = direction;

        // The leading '+' (force) marker is part of the specification but not of the
        // source/destination patterns, matching libgit2's refspec parsing.
        var body = specification.StartsWith('+') ? specification[1..] : specification;
        var separator = body.IndexOf(':');

        if (separator < 0)
        {
            Source = body;
            Destination = string.Empty;
        }
        else
        {
            Source = body[..separator];
            Destination = body[(separator + 1)..];
        }
    }

    public string Specification { get; }
    public RefSpecDirection Direction { get; }
    public string Source { get; }
    public string Destination { get; }

    public int CompareTo(IRefSpec? other) => comparerHelper.Compare(this, other);
    public bool Equals(IRefSpec? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals(obj as IRefSpec);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Specification;
}
