using System;
using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitRepository : IDisposable
    {
        IGitRepositoryCommands Commands { get; }
        Branch Head { get; }
        CommitCollection Commits { get; }
        BranchCollection Branches { get; }
        TagCollection Tags { get; }
        ReferenceCollection Refs { get; }
        RepositoryInformation Info { get; }
        Network Network { get; }
        int GetNumberOfUncommittedChanges();
        Commit FindMergeBase(Commit commit, Commit otherCommit);
        string ShortenObjectId(Commit commit);
    }
}
