using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp;

public class MockQueryableCommitLog : IQueryableCommitLog
{
    readonly ICommitLog commits;

    public MockQueryableCommitLog(ICommitLog commits)
    {
        this.commits = commits;
    }

    public IEnumerator<Commit> GetEnumerator()
    {
        return commits.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public CommitSortStrategies SortedBy
    {
        get { throw new NotImplementedException(); }
    }

    public ICommitLog QueryBy(CommitFilter filter)
    {
        return this;
    }

    public IEnumerable<LogEntry> QueryBy(string path)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<LogEntry> QueryBy(string path, FollowFilter filter)
    {
        throw new NotImplementedException();
    }

    public Commit FindMergeBase(Commit first, Commit second)
    {
        return null;
    }

    public Commit FindMergeBase(IEnumerable<Commit> commits, MergeBaseFindingStrategy strategy)
    {
        throw new NotImplementedException();
    }
}