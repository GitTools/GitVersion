namespace GitVersion.Git;

public interface ICommitCollection : IEnumerable<ICommit>
{
    IEnumerable<ICommit> GetCommitsPriorTo(DateTimeOffset olderThan);
    IEnumerable<ICommit> QueryBy(CommitFilter commitFilter);
}
