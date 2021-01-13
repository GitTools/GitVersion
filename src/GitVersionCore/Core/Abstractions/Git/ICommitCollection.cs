using System;
using System.Collections.Generic;

namespace GitVersion
{
    public interface ICommitCollection : IEnumerable<ICommit>
    {
        IEnumerable<ICommit> GetCommitsPriorTo(DateTimeOffset olderThan);
        ICommitCollection QueryBy(CommitFilter commitFilter);
    }
}
