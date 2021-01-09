using LibGit2Sharp;

namespace GitVersion
{
    public class Reference : IReference
    {
        internal readonly LibGit2Sharp.Reference innerReference;
        private DirectReference directReference => innerReference.ResolveToDirectReference();

        internal Reference(LibGit2Sharp.Reference reference)
        {
            innerReference = reference;
        }

        protected Reference()
        {
        }
        public virtual string CanonicalName => innerReference.CanonicalName;
        public virtual string TargetIdentifier => innerReference.TargetIdentifier;
        public virtual string DirectReferenceTargetIdentifier => directReference.TargetIdentifier;
        public virtual IObjectId DirectReferenceTargetId => (ObjectId)directReference.Target.Id;
        public virtual IReference ResolveToDirectReference() => new Reference(directReference);
        public static implicit operator LibGit2Sharp.Reference(Reference d) => d?.innerReference;
    }
}
