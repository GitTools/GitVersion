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

        int GetNumberOfUncommittedChanges();
        string ShortenObjectId(ICommit commit);

        bool GitRepoHasMatchingRemote(string targetUrl);
        void CleanupDuplicateOrigin(string defaultRemoteName);
        bool GetMatchingCommitBranch(ICommit baseVersionSource, IBranch branch, ICommit firstMatchingCommit);
        IRemote EnsureOnlyOneRemoteIsDefined();
        ICommit FindMergeBase(ICommit commit, ICommit otherCommit);
        ICommit GetBaseVersionSource(ICommit currentBranchTip);
        ICommit GetForwardMerge(ICommit commitToFindCommonBase, ICommit findMergeBase);

        IEnumerable<ICommit> GetCommitsReacheableFrom(ICommit commit, IBranch branch);
        IEnumerable<ICommit> GetMergeBaseCommits(ICommit mergeCommit, ICommit mergedHead, ICommit findMergeBase);
        IEnumerable<ICommit> GetMainlineCommitLog(ICommit baseVersionSource, ICommit mainlineTip);
        IEnumerable<ICommit> GetCommitsReacheableFromHead(ICommit headCommit);
        IEnumerable<ICommit> GetCommitLog(ICommit baseVersionSource, ICommit currentCommit);

        void Checkout(string commitOrBranchSpec);
        void Checkout(IBranch branch);
        void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string logMessage);
        void CreateBranchForPullRequestBranch(AuthenticationInfo auth);
    }
}
