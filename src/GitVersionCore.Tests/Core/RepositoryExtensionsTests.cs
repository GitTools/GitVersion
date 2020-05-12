using GitVersion.Logging;
using GitVersion.Extensions;
using GitVersionCore.Tests.Helpers;
using LibGit2Sharp;
using NUnit.Framework;
using NSubstitute;
using System;
using System.Collections.Generic;
using GitVersion;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class RepositoryExtensionsTests : TestBase
    {
        [Test]
        public void EnsureLocalBranchExistsForCurrentBranch_CaseInsensitivelyMatchesBranches()
        {
            var log = Substitute.For<ILog>();
            var repository = MockRepository();
            var remote = MockRemote(repository);

            repository.EnsureLocalBranchExistsForCurrentBranch(log, remote, "refs/heads/featurE/feat-test");
        }

        private IGitRepository MockRepository()
        {
            var repository = Substitute.For<IGitRepository>();
            var commands = Substitute.For<IGitRepositoryCommands>();
            repository.Commands.Returns(commands);
            return repository;
        }

        private Remote MockRemote(IGitRepository repository)
        {
            var branches = new TestableBranchCollection(repository);
            var tipId = new ObjectId("c6d8764d20ff16c0df14c73680e52b255b608926");
            var tip = new TestableCommit(repository, tipId);
            var head = branches.Add("refs/heads/feature/feat-test", tip);
            var remote = new TesatbleRemote("origin");
            var references = new TestableReferenceCollection();
            _ = references.Add("develop", "refs/heads/develop");

            repository.Refs.Returns(references);
            repository.Head.Returns(head);
            repository.Branches.Returns(branches);
            return remote;
        }

        private class TestableBranchCollection : BranchCollection
        {
            private readonly IRepository repository;
            public TestableBranchCollection(IRepository repository)
            {
            }

            IDictionary<string, Branch> branches = new Dictionary<string, Branch>();

            public override Branch this[string name] =>
                this.branches.ContainsKey(name)
                    ? this.branches[name]
                    : null;

            public override Branch Add(string name, Commit commit)
            {
                var branch = new TestableBranch(name, commit);
                this.branches.Add(name, branch);
                return branch;
            }

            public override Branch Add(string name, string committish)
            {
                var id = new ObjectId(committish);
                var commit = new TestableCommit(this.repository, id);
                return Add(name, commit);
            }

            public override Branch Add(string name, Commit commit, bool allowOverwrite)
            {
                return Add(name, commit);
            }

            public override Branch Add(string name, string committish, bool allowOverwrite)
            {
                return Add(name, committish);
            }

            public override IEnumerator<Branch> GetEnumerator()
            {
                return this.branches.Values.GetEnumerator();
            }

            public override void Remove(string name)
            {
                this.branches.Remove(name);
            }

            public override void Remove(string name, bool isRemote)
            {
                this.branches.Remove(name);
            }

            public override void Remove(Branch branch)
            {
                this.branches.Remove(branch.CanonicalName);
            }

            public override Branch Update(Branch branch, params Action<BranchUpdater>[] actions)
            {
                return base.Update(branch, actions);
            }
        }

        private class TestableBranch : Branch
        {
            private readonly string canonicalName;
            private readonly Commit tip;

            public TestableBranch(string canonicalName, Commit tip)
            {
                this.tip = tip;
                this.canonicalName = canonicalName;
            }

            public override string CanonicalName => this.canonicalName;
            public override Commit Tip => this.tip;
        }

        private class TestableCommit : Commit, IBelongToARepository
        {
            private IRepository repository;
            private ObjectId id;

            public TestableCommit(IRepository repository, ObjectId id)
            {
                this.repository = repository;
                this.id = id;
            }

            public override ObjectId Id => this.id;
            public IRepository Repository => this.repository;
        }

        private class TesatbleRemote : Remote
        {
            private string name;

            public TesatbleRemote(string name)
            {
                this.name = name;
            }

            public override string Name => this.name;
        }

        private class TestableReferenceCollection : ReferenceCollection
        {
            Reference reference;

            public override DirectReference Add(string name, ObjectId targetId)
            {
                throw new InvalidOperationException("Update should be invoked when case-insensitively comparing branches.");
            }

            public override Reference Add(string name, string canonicalRefNameOrObjectish)
            {
                return this.reference = new TestableReference(canonicalRefNameOrObjectish);
            }

            public override Reference UpdateTarget(Reference directRef, ObjectId targetId)
            {
                return this.reference;
            }

            public override Reference this[string name] => this.reference;
        }

        private class TestableReference : Reference
        {
            private readonly string canonicalName;

            public TestableReference(string canonicalName)
            {
                this.canonicalName = canonicalName;
            }

            public override string CanonicalName => this.canonicalName;

            public override DirectReference ResolveToDirectReference()
            {
                throw new NotImplementedException();
            }
        }
    }
}
