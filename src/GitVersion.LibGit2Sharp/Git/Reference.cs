using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion
{
    internal sealed class Reference : IReference
    {
        private static readonly LambdaEqualityHelper<IReference> equalityHelper = new(x => x.Name.CanonicalName);
        private static readonly LambdaKeyComparer<IReference, string> comparerHelper = new(x => x.Name.CanonicalName);

        internal readonly LibGit2Sharp.Reference innerReference;
        private DirectReference directReference => innerReference.ResolveToDirectReference();

        internal Reference(LibGit2Sharp.Reference reference)
        {
            innerReference = reference;
            Name = new ReferenceName(reference.CanonicalName);
        }
        public ReferenceName Name { get; }
        public int CompareTo(IReference other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals((obj as IReference)!);
        public bool Equals(IReference other) => equalityHelper.Equals(this, other);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);
        public string TargetIdentifier => innerReference.TargetIdentifier;
        public string DirectReferenceTargetIdentifier => directReference.TargetIdentifier;
        public IObjectId DirectReferenceTargetId => new ObjectId(directReference.Target.Id);
        public static implicit operator LibGit2Sharp.Reference(Reference d) => d.innerReference;
    }
}
