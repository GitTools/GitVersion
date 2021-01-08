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
        TagCollection Tags { get; }
        ReferenceCollection Refs { get; }

        int GetNumberOfUncommittedChanges();
        Commit FindMergeBase(Commit commit, Commit otherCommit);
        string ShortenObjectId(Commit commit);

        bool GitRepoHasMatchingRemote(string targetUrl);
        void CleanupDuplicateOrigin(string defaultRemoteName);
        bool GetMatchingCommitBranch(Commit baseVersionSource, IBranch branch, Commit firstMatchingCommit);
        IEnumerable<Commit> GetCommitsReacheableFrom(Commit commit, IBranch branch);
        List<Commit> GetCommitsReacheableFromHead(Commit headCommit);
        Commit GetForwardMerge(Commit commitToFindCommonBase, Commit findMergeBase);
        IEnumerable<Commit> GetMergeBaseCommits(Commit mergeCommit, Commit mergedHead, Commit findMergeBase);
        Commit GetBaseVersionSource(Commit currentBranchTip);
        List<Commit> GetMainlineCommitLog(Commit baseVersionSource, Commit mainlineTip);
        CommitCollection GetCommitLog(Commit baseVersionSource, Commit currentCommit);

        void Checkout(string committishOrBranchSpec);
        void Checkout(IBranch branch);
        void Fetch(string remote, IEnumerable<string> refspecs, AuthenticationInfo auth, string logMessage);
        void CreateBranchForPullRequestBranch(ILog log, AuthenticationInfo auth);
        IRemote EnsureOnlyOneRemoteIsDefined(ILog log);
    }
}
