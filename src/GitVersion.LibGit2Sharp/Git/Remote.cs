using System.Linq;
using GitVersion.Helpers;

namespace GitVersion
{
    internal sealed class Remote : IRemote
    {
        private static readonly LambdaEqualityHelper<IRemote> equalityHelper = new(x => x.Name);
        private static readonly LambdaKeyComparer<IRemote, string> comparerHelper = new(x => x.Name);

        private readonly LibGit2Sharp.Remote innerRemote;

        internal Remote(LibGit2Sharp.Remote remote) => innerRemote = remote;

        public int CompareTo(IRemote other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals((obj as IRemote)!);
        public bool Equals(IRemote other) => equalityHelper.Equals(this, other);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public string Name => innerRemote.Name;
        public string RefSpecs => string.Join(", ", innerRemote.FetchRefSpecs.Select(r => r.Specification));
    }
}
