using GitVersion.Helpers;
using LibGit2Sharp;

namespace GitVersion
{
    internal class Reference : IReference
    {
        private static readonly LambdaEqualityHelper<IReference> equalityHelper =
            new LambdaEqualityHelper<IReference>(x => x.CanonicalName);
        private static readonly LambdaKeyComparer<IReference, string> comparerHelper =
            new LambdaKeyComparer<IReference, string>(x => x.CanonicalName);

        internal readonly LibGit2Sharp.Reference innerReference;
        private DirectReference directReference => innerReference.ResolveToDirectReference();

        internal Reference(LibGit2Sharp.Reference reference)
        {
            innerReference = reference;
        }

        protected Reference()
        {
        }

        public int CompareTo(IReference other) => comparerHelper.Compare(this, other);
        public override bool Equals(object obj) => Equals(obj as IReference);
        public bool Equals(IReference other) => equalityHelper.Equals(this, other);
        public override int GetHashCode() => equalityHelper.GetHashCode(this);

        public virtual string CanonicalName => innerReference.CanonicalName;
        public virtual string TargetIdentifier => innerReference.TargetIdentifier;
        public virtual string DirectReferenceTargetIdentifier => directReference.TargetIdentifier;
        public virtual IObjectId DirectReferenceTargetId => new ObjectId(directReference.Target.Id);
        public virtual IReference ResolveToDirectReference() => new Reference(directReference);
        public static implicit operator LibGit2Sharp.Reference(Reference d) => d.innerReference;
    }
}
