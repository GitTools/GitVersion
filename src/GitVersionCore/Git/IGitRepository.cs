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

        ICommit FindMergeBase(ICommit commit, ICommit otherCommit);
        int GetNumberOfUncommittedChanges();
        void CreateBranchForPullRequestBranch(AuthenticationInfo auth);

        void Checkout(string commitOrBranchSpec);
        void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string logMessage);
        void Clone(string sourceUrl, string workdirPath, AuthenticationInfo auth);
    }
}
