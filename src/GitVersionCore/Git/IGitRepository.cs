using System;
using System.Collections.Generic;

namespace GitVersion
{
    public interface IGitRepository : IDisposable
    {
        string Path { get; }
        string WorkingDirectory { get; }
        bool IsHeadDetached { get; }
        IBranch Head { get; }
        ITagCollection Tags { get; }
        IReferenceCollection Refs { get; }
        IBranchCollection Branches { get; }
        ICommitCollection Commits { get; }
        IRemoteCollection Remotes { get; }

        int GetNumberOfUncommittedChanges();
        string ShortenObjectId(ICommit commit);

        void CleanupDuplicateOrigin(string remoteName);
        IRemote EnsureOnlyOneRemoteIsDefined();
        ICommit FindMergeBase(ICommit commit, ICommit otherCommit);

        void Checkout(string commitOrBranchSpec);
        void Checkout(IBranch branch);
        void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string logMessage);
        void CreateBranchForPullRequestBranch(AuthenticationInfo auth);
        string Clone(string sourceUrl, string workdirPath, AuthenticationInfo auth);
    }
}
