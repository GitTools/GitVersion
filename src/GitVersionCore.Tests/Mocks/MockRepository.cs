using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion;
using NSubstitute;

namespace GitVersionCore.Tests.Mocks
{
    public class MockRepository : IGitRepository
    {
        private ICommitCollection commits;
        public IBranch Head { get; set; }
        public ITagCollection Tags => Substitute.For<ITagCollection>();
        public IReferenceCollection Refs => Substitute.For<IReferenceCollection>();

        public IBranchCollection Branches { get; set; }
        public ICommitCollection Commits
        {
            get => commits ?? Head.Commits;
            set => commits = value;
        }

        public string Path { get; }
        public string WorkingDirectory { get; }
        public bool IsHeadDetached { get; }
        public IGitRepository CreateNew(string gitRootPath)
        {
            throw new NotImplementedException();
        }
        public int GetNumberOfUncommittedChanges() => 0;
        public ICommit FindMergeBase(ICommit commit, ICommit otherCommit) => throw new NotImplementedException();
        public string ShortenObjectId(ICommit commit) => throw new NotImplementedException();
        public bool GitRepoHasMatchingRemote(string targetUrl) => throw new NotImplementedException();
        public void CleanupDuplicateOrigin(string gitRootPath, string remoteName) => throw new NotImplementedException();
        public bool GetMatchingCommitBranch(ICommit baseVersionSource, IBranch branch, ICommit firstMatchingCommit)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<ICommit> GetCommitsReacheableFrom(ICommit commit, IBranch branch)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<ICommit> GetCommitsReacheableFromHead(ICommit headCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = headCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            };

            var commitCollection = Commits.QueryBy(filter);

            return commitCollection.ToList();
        }
        public ICommit GetForwardMerge(ICommit commitToFindCommonBase, ICommit findMergeBase)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<ICommit> GetMergeBaseCommits(ICommit mergeCommit, ICommit mergedHead, ICommit findMergeBase)
        {
            throw new NotImplementedException();
        }
        public ICommit GetBaseVersionSource(ICommit currentBranchTip)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<ICommit> GetMainlineCommitLog(ICommit baseVersionSource, ICommit mainlineTip)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<ICommit> GetCommitLog(ICommit baseVersionSource, ICommit currentCommit)
        {
            throw new NotImplementedException();
        }
        public void Checkout(string commitOrBranchSpec)
        {
            throw new NotImplementedException();
        }
        public void Checkout(IBranch branch)
        {
            throw new NotImplementedException();
        }
        public void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string logMessage)
        {
            throw new NotImplementedException();
        }
        public void CreateBranchForPullRequestBranch(AuthenticationInfo auth)
        {
            throw new NotImplementedException();
        }
        public string Clone(string sourceUrl, string workdirPath, AuthenticationInfo auth)
        {
            throw new NotImplementedException();
        }
        public IRemote EnsureOnlyOneRemoteIsDefined() => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
    }
}
