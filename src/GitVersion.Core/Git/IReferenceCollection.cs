using GitVersion.Logging;
using System;
using System.Collections.Generic;

namespace GitVersion
{
    public interface IReferenceCollection : IEnumerable<IReference>
    {
        IReference Head { get; }
        IReference this[string name] { get; }
        void Add(string name, string canonicalRefNameOrObjectish, bool allowOverwrite = false);
        void UpdateTarget(IReference directRef, IObjectId targetId);
        IEnumerable<IReference> FromGlob(string prefix);
    }

    public class LockedFileException : Exception
    {
        public LockedFileException(Exception inner) : base(inner.Message, inner)
        {
        }
    }
}
