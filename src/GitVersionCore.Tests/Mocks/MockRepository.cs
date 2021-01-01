using System;
using GitVersion;
using LibGit2Sharp;

namespace GitVersionCore.Tests.Mocks
{
    public class MockRepository : IGitRepository
    {
        private IQueryableCommitLog commits;

        public MockRepository()
        {
            Tags = new MockTagCollection();
            Refs = new MockReferenceCollection();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Branch Head { get; set; }
        public ReferenceCollection Refs { get; set; }

        public IQueryableCommitLog Commits
        {
            get => commits ?? new MockQueryableCommitLog(Head.Commits);
            set => commits = value;
        }

        public BranchCollection Branches { get; set; }
        public TagCollection Tags { get; set; }
        public RepositoryInformation Info { get; set; }
        public Diff Diff { get; set; }
        public ObjectDatabase ObjectDatabase { get; set; }

        public Network Network { get; set; }
        public RepositoryStatus RetrieveStatus()
        {
            throw new NotImplementedException();
        }
        public IGitRepositoryCommands Commands { get; }
    }
}
