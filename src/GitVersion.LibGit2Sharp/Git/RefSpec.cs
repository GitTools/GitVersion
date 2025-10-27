using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class RefSpec : IRefSpec
{
    private static readonly LambdaEqualityHelper<IRefSpec> equalityHelper = new(x => x.Specification);
    private static readonly LambdaKeyComparer<IRefSpec, string> comparerHelper = new(x => x.Specification);
    private readonly LibGit2Sharp.RefSpec innerRefSpec;

    internal RefSpec(LibGit2Sharp.RefSpec refSpec) => this.innerRefSpec = refSpec.NotNull();
    public int CompareTo(IRefSpec? other) => comparerHelper.Compare(this, other);
    public bool Equals(IRefSpec? other) => equalityHelper.Equals(this, other);
    public string Specification => this.innerRefSpec.Specification;
    public RefSpecDirection Direction => (RefSpecDirection)this.innerRefSpec.Direction;
    public string Source => this.innerRefSpec.Source;
    public string Destination => this.innerRefSpec.Destination;
    public override bool Equals(object? obj) => Equals(obj as IRefSpec);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Specification;
}
