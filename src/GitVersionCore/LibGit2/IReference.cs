namespace GitVersion
{
    public interface IReference
    {
        string CanonicalName { get; }
        string TargetIdentifier { get; }
        string DirectReferenceTargetIdentifier { get; }
        IObjectId DirectReferenceTargetId { get; }
        IReference ResolveToDirectReference();
    }
}
