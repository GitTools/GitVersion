using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion
{
    internal sealed class Reference : IReference
    {
        private static readonly LambdaEqualityHelper<IReference> equalityHelper = new(x => x.Name.Canonical);
        private static readonly LambdaKeyComparer<IReference, string> comparerHelper = new(x => x.Name.Canonical);

        internal readonly LibGit2Sharp.Reference innerReference;

        internal Reference(LibGit2Sharp.Reference reference)
        {
            this.innerReference = reference;
            Name = new ReferenceName(reference.CanonicalName);

            if (reference is DirectReference)
                ReferenceTargetId = new ObjectId(reference.TargetIdentifier);
        }
        public ReferenceName Name { get; }
        public IObjectId? ReferenceTargetId { get; }
        public int CompareTo(IReference other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals((obj as IReference)!);
        public bool Equals(IReference? other) => equalityHelper.Equals(this, other);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public override string ToString() => Name.ToString();
        public string TargetIdentifier => this.innerReference.TargetIdentifier;
        public static implicit operator LibGit2Sharp.Reference(Reference d) => d.innerReference;
    }
}
