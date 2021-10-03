namespace GitVersion;

public interface IReferenceCollection : IEnumerable<IReference>
{
    IReference? Head { get; }
    IReference? this[string name] { get; }
    void Add(string name, string canonicalRefNameOrObjectish, bool allowOverwrite = false);
    void UpdateTarget(IReference directRef, IObjectId targetId);
    IEnumerable<IReference> FromGlob(string prefix);
}
