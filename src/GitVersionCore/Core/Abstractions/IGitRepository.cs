using System;
using GitVersion.Logging;
using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitRepository : IDisposable
    {
        string Path { get; }
        string WorkingDirectory { get; }
        bool IsHeadDetached { get; }
        IGitRepositoryCommands Commands { get; }
        Branch Head { get; }
        CommitCollection Commits { get; }
        BranchCollection Branches { get; }
        TagCollection Tags { get; }
        ReferenceCollection Refs { get; }

        int GetNumberOfUncommittedChanges();
        Commit FindMergeBase(Commit commit, Commit otherCommit);
        string ShortenObjectId(Commit commit);
        void CreateBranchForPullRequestBranch(ILog log, AuthenticationInfo auth);
        Remote EnsureOnlyOneRemoteIsDefined(ILog log);
        bool GitRepoHasMatchingRemote(string targetUrl);
        void CleanupDuplicateOrigin(string defaultRemoteName);
    }
}
