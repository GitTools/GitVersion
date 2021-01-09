using System;
using System.Collections.Generic;
using GitVersion.Logging;

namespace GitVersion
{
    public interface IGitRepository : IDisposable
    {
        string Path { get; }
        string WorkingDirectory { get; }
        bool IsHeadDetached { get; }
        IBranch Head { get; }
        CommitCollection Commits { get; }
        BranchCollection Branches { get; }
        IEnumerable<ITag> Tags { get; }
        ReferenceCollection Refs { get; }

        int GetNumberOfUncommittedChanges();
        ICommit FindMergeBase(ICommit commit, ICommit otherCommit);
        string ShortenObjectId(ICommit commit);

        bool GitRepoHasMatchingRemote(string targetUrl);
        void CleanupDuplicateOrigin(string defaultRemoteName);
        bool GetMatchingCommitBranch(ICommit baseVersionSource, IBranch branch, ICommit firstMatchingCommit);
        IEnumerable<ICommit> GetCommitsReacheableFrom(ICommit commit, IBranch branch);
        List<ICommit> GetCommitsReacheableFromHead(ICommit headCommit);
        ICommit GetForwardMerge(ICommit commitToFindCommonBase, ICommit findMergeBase);
        IEnumerable<ICommit> GetMergeBaseCommits(ICommit mergeCommit, ICommit mergedHead, ICommit findMergeBase);
        ICommit GetBaseVersionSource(ICommit currentBranchTip);
        List<ICommit> GetMainlineCommitLog(ICommit baseVersionSource, ICommit mainlineTip);
        CommitCollection GetCommitLog(ICommit baseVersionSource, ICommit currentCommit);

        void Checkout(string commitOrBranchSpec);
        void Checkout(IBranch branch);
        void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string logMessage);
        void CreateBranchForPullRequestBranch(ILog log, AuthenticationInfo auth);
        IRemote EnsureOnlyOneRemoteIsDefined(ILog log);
    }
}
