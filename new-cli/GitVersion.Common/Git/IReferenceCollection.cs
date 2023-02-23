namespace GitVersion;

public interface IReferenceCollection : IEnumerable<IReference>
{
    IReference? Head { get; }
    IReference? this[string name] { get; }
    IReference? this[ReferenceName referenceName] { get; }
    void Add(string name, string canonicalRefNameOrObject, bool allowOverwrite = false);
    void UpdateTarget(IReference directRef, IObjectId targetId);
    IEnumerable<IReference> FromGlob(string prefix);
}
