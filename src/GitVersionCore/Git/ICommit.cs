using System;
using System.Collections.Generic;

namespace GitVersion
{
    public interface ICommit : IEquatable<ICommit>, IComparable<ICommit>
    {
        IEnumerable<ICommit> Parents { get; }
        string Sha { get; }
        IObjectId Id { get; }
        DateTimeOffset When { get; }
        string Message { get; }
    }
}
