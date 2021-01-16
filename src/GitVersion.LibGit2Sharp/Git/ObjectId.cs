using GitVersion.Helpers;

namespace GitVersion
{
    internal sealed class ObjectId : IObjectId
    {
        private static readonly LambdaEqualityHelper<IObjectId> equalityHelper = new(x => x.Sha);
        private static readonly LambdaKeyComparer<IObjectId, string> comparerHelper = new(x => x.Sha);

        private readonly LibGit2Sharp.ObjectId innerObjectId;
        internal ObjectId(LibGit2Sharp.ObjectId objectId) => innerObjectId = objectId;

        public ObjectId(string sha) : this(new LibGit2Sharp.ObjectId(sha))
        {
        }

        public int CompareTo(IObjectId other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals((obj as IObjectId)!);
        public bool Equals(IObjectId other) => equalityHelper.Equals(this, other);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public static implicit operator LibGit2Sharp.ObjectId(ObjectId d) => d.innerObjectId;
        public string Sha => innerObjectId.Sha;

        public string ToString(int prefixLength) => innerObjectId.ToString(prefixLength);
    }
}
