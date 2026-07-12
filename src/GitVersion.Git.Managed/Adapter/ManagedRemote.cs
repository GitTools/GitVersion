using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Git;

internal sealed class ManagedRemote : IRemote
{
    private static readonly LambdaEqualityHelper<IRemote> equalityHelper = new(x => x.Name);
    private static readonly LambdaKeyComparer<IRemote, string> comparerHelper = new(x => x.Name);

    private readonly ManagedRefSpecCollection refSpecs;

    internal ManagedRemote(string name, string url, IReadOnlyList<string> fetchRefSpecs, IReadOnlyList<string> pushRefSpecs)
    {
        Name = name.NotNull();
        Url = url.NotNull();
        this.refSpecs = new(
        [
            .. fetchRefSpecs.Select(IRefSpec (spec) => new ManagedRefSpec(spec, RefSpecDirection.Fetch)),
            .. pushRefSpecs.Select(IRefSpec (spec) => new ManagedRefSpec(spec, RefSpecDirection.Push))
        ]);
    }

    public string Name { get; }
    public string Url { get; }

    public IEnumerable<IRefSpec> FetchRefSpecs => this.refSpecs.Where(x => x.Direction == RefSpecDirection.Fetch);
    public IEnumerable<IRefSpec> PushRefSpecs => this.refSpecs.Where(x => x.Direction == RefSpecDirection.Push);

    public int CompareTo(IRemote? other) => comparerHelper.Compare(this, other);
    public bool Equals(IRemote? other) => equalityHelper.Equals(this, other);
    public override bool Equals(object? obj) => Equals(obj as IRemote);
    public override int GetHashCode() => equalityHelper.GetHashCode(this);
    public override string ToString() => Name;
}
