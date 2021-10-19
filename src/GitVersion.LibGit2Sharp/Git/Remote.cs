using GitVersion.Helpers;

namespace GitVersion;

internal sealed class Remote : IRemote
{
    private static readonly LambdaEqualityHelper<IRemote> equalityHelper = new(x => x.Name);
    private static readonly LambdaKeyComparer<IRemote, string> comparerHelper = new(x => x.Name);

    private readonly LibGit2Sharp.Remote innerRemote;

    internal Remote(LibGit2Sharp.Remote remote) => this.innerRemote = remote;

    public int CompareTo(IRemote other) => comparerHelper.Compare(this, other);
    public bool Equals(IRemote? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object obj) => Equals((obj as IRemote)!);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name;
    public string Name => this.innerRemote.Name;
    public string Url => this.innerRemote.Url;

    public IEnumerable<IRefSpec> RefSpecs
    {
        get
        {
            var refSpecs = this.innerRemote.RefSpecs;
            return refSpecs is null
                ? Enumerable.Empty<IRefSpec>()
                : new RefSpecCollection((LibGit2Sharp.RefSpecCollection)refSpecs);
        }
    }
    public IEnumerable<IRefSpec> FetchRefSpecs => RefSpecs.Where(x => x.Direction == RefSpecDirection.Fetch);

    public IEnumerable<IRefSpec> PushRefSpecs => RefSpecs.Where(x => x.Direction == RefSpecDirection.Push);
}
