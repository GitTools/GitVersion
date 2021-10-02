using System;
using System.Collections.Generic;

namespace GitVersion;

public interface ICommit : IEquatable<ICommit?>, IComparable<ICommit>, IGitObject
{
    IEnumerable<ICommit> Parents { get; }
    DateTimeOffset When { get; }
    string Message { get; }
}