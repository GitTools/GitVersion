using System;
using GitVersion;
using LibGit2Sharp;
using Branch = GitVersion.Branch;
using BranchCollection = GitVersion.BranchCollection;
using Commit = GitVersion.Commit;
using ReferenceCollection = GitVersion.ReferenceCollection;
using TagCollection = GitVersion.TagCollection;

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
        public Network Network { get; set; }
        public string Path { get; }
        public bool IsHeadDetached { get; }
        public int GetNumberOfUncommittedChanges() => 0;
        public Commit FindMergeBase(Commit commit, Commit otherCommit) => throw new NotImplementedException();
        public string ShortenObjectId(Commit commit) => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
    }
}
