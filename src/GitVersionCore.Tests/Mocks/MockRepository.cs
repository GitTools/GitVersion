using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion;
using GitVersion.Logging;
namespace GitVersionCore.Tests.Mocks
{
    public class MockRepository : IGitRepository
    {
        private CommitCollection commits;
        public IGitRepositoryCommands Commands { get; }

        public MockRepository()
        {
            Tags = new MockTagCollection();
            Refs = new MockReferenceCollection();
        }
        public Branch Head { get; set; }
        public ReferenceCollection Refs { get; set; }

        public CommitCollection Commits
        {
            get => commits ?? Head.Commits;
            set => commits = value;
        }

        public BranchCollection Branches { get; set; }
        public TagCollection Tags { get; set; }
        public string Path { get; }
        public string WorkingDirectory { get; }
        public bool IsHeadDetached { get; }
        public int GetNumberOfUncommittedChanges() => 0;
        public Commit FindMergeBase(Commit commit, Commit otherCommit) => throw new NotImplementedException();
        public string ShortenObjectId(Commit commit) => throw new NotImplementedException();
        public void CreateBranchForPullRequestBranch(ILog log, AuthenticationInfo auth) => throw new NotImplementedException();
        public bool GitRepoHasMatchingRemote(string targetUrl) => throw new NotImplementedException();
        public void CleanupDuplicateOrigin(string defaultRemoteName) => throw new NotImplementedException();
        public bool GetMatchingCommitBranch(Commit baseVersionSource, Branch branch, Commit firstMatchingCommit)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<Commit> GetCommitsReacheableFrom(Commit commit, Branch branch)
        {
            throw new NotImplementedException();
        }
        public List<Commit> GetCommitsReacheableFromHead(Commit headCommit)
        {
            var filter = new CommitFilter
            {
                IncludeReachableFrom = headCommit,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
            };

            var commitCollection = Commits.QueryBy(filter);

            return commitCollection.ToList();
        }
        public Commit GetForwardMerge(Commit commitToFindCommonBase, Commit findMergeBase)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<Commit> GetMergeBaseCommits(Commit mergeCommit, Commit mergedHead, Commit findMergeBase)
        {
            throw new NotImplementedException();
        }
        public Commit GetBaseVersionSource(Commit currentBranchTip)
        {
            throw new NotImplementedException();
        }
        public List<Commit> GetMainlineCommitLog(Commit baseVersionSource, Commit mainlineTip)
        {
            throw new NotImplementedException();
        }
        public CommitCollection GetCommitLog(Commit baseVersionSource, Commit currentCommit)
        {
            throw new NotImplementedException();
        }
        public Remote EnsureOnlyOneRemoteIsDefined(ILog log) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
    }
}
