using GitVersion.Git;
using GitVersion.Helpers;

namespace GitVersion;

public class RefSpec : IRefSpec
{
    private static readonly LambdaEqualityHelper<IRefSpec> EqualityHelper = new(x => x.Specification);
    private static readonly LambdaKeyComparer<IRefSpec, string> ComparerHelper = new(x => x.Specification);

    private readonly LibGit2Sharp.RefSpec innerRefSpec;

    internal RefSpec(LibGit2Sharp.RefSpec refSpec) => this.innerRefSpec = refSpec;
    public int CompareTo(IRefSpec? other) => ComparerHelper.Compare(this, other);
    public bool Equals(IRefSpec? other) => EqualityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals((obj as IRefSpec));
    public override int GetHashCode() => EqualityHelper.GetHashCode(this);
    public override string ToString() => Specification;
    public string Specification => this.innerRefSpec.Specification;
    public RefSpecDirection Direction => (RefSpecDirection)this.innerRefSpec.Direction;
    public string Source => this.innerRefSpec.Source;
    public string Destination => this.innerRefSpec.Destination;
}
