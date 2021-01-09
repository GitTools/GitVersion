using System.Collections.Generic;

namespace GitVersion
{
    public interface IReferenceCollection : IEnumerable<IReference>
    {
        IReference Head { get; }
        IReference this[string localCanonicalName] { get; }
        void Add(string name, string canonicalRefNameOrObjectish, bool allowOverwrite = false);
        void UpdateTarget(IReference directRef, IObjectId targetId);
        IEnumerable<IReference> FromGlob(string prefix);
    }
}
