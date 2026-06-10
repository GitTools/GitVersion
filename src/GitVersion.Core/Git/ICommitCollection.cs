namespace GitVersion.Git;

/// <summary>Represents an ordered, queryable collection of commits in a Git repository.</summary>
public interface ICommitCollection : IEnumerable<ICommit>
{
    /// <summary>Returns all commits whose author date is earlier than <paramref name="olderThan"/>.</summary>
    IEnumerable<ICommit> GetCommitsPriorTo(DateTimeOffset olderThan);

    /// <summary>Returns commits matching the supplied <paramref name="commitFilter"/>.</summary>
    IEnumerable<ICommit> QueryBy(CommitFilter commitFilter);
}
