using System;
using System.Collections.Generic;

namespace GitVersion
{
    public interface ICommit
    {
        IEnumerable<ICommit> Parents { get; }
        string Sha { get; }
        IObjectId Id { get; }
        DateTimeOffset? CommitterWhen { get; }
        string Message { get; }
    }
}
