using System.Collections.Generic;

namespace GitVersion
{
    public interface ICommitCollection : IEnumerable<ICommit>
    {
        ICommitCollection QueryBy(CommitFilter commitFilter);
    }
}
