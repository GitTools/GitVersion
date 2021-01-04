using System;
using GitVersion;
using GitVersion.Logging;
using Remote = LibGit2Sharp.Remote;
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
        public Remote EnsureOnlyOneRemoteIsDefined(ILog log) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
    }
}
