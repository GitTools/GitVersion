using System;
using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitRepository : IDisposable
    {
        string Path { get; }
        bool IsHeadDetached { get; }
        IGitRepositoryCommands Commands { get; }
        Branch Head { get; }
        CommitCollection Commits { get; }
        BranchCollection Branches { get; }
        TagCollection Tags { get; }
        ReferenceCollection Refs { get; }
        Network Network { get; }
        int GetNumberOfUncommittedChanges();
        Commit FindMergeBase(Commit commit, Commit otherCommit);
        string ShortenObjectId(Commit commit);
    }
}
