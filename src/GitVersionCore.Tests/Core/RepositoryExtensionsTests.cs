using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersionCore.Tests.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace GitVersionCore.Tests
{
    [TestFixture]
    public class RepositoryExtensionsTests : TestBase
    {
        private static void EnsureLocalBranchExistsForCurrentBranch(IGitRepository repo, ILog log, Remote remote, string currentBranch)
        {
            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            if (remote is null)
            {
                throw new ArgumentNullException(nameof(remote));
            }

            if (string.IsNullOrEmpty(currentBranch)) return;

            var isRef = currentBranch.Contains("refs");
            var isBranch = currentBranch.Contains("refs/heads");
            var localCanonicalName = !isRef
                ? "refs/heads/" + currentBranch
                : isBranch
                    ? currentBranch
                    : currentBranch.Replace("refs/", "refs/heads/");

            var repoTip = repo.Head.Tip;

            // We currently have the rep.Head of the *default* branch, now we need to look up the right one
            var originCanonicalName = $"{remote.Name}/{currentBranch}";
            var originBranch = repo.Branches[originCanonicalName];
            if (originBranch != null)
            {
                repoTip = originBranch.Tip;
            }

            var repoTipId = repoTip.Id;

            if (repo.Branches.All(b => !b.CanonicalName.IsEquivalentTo(localCanonicalName)))
            {
                log.Info(isBranch ? $"Creating local branch {localCanonicalName}"
                    : $"Creating local branch {localCanonicalName} pointing at {repoTipId}");
                repo.Refs.Add(localCanonicalName, repoTipId);
            }
            else
            {
                log.Info(isBranch ? $"Updating local branch {localCanonicalName} to point at {repoTip.Sha}"
                    : $"Updating local branch {localCanonicalName} to match ref {currentBranch}");
                var localRef = repo.Refs[localCanonicalName];
                repo.Refs.UpdateTarget(localRef, repoTipId);
            }

            repo.Checkout(localCanonicalName);
        }

        [Test]
        public void EnsureLocalBranchExistsForCurrentBranch_CaseInsensitivelyMatchesBranches()
        {
            var log = Substitute.For<ILog>();
            var repository = MockRepository();
            var remote = MockRemote(repository);

            EnsureLocalBranchExistsForCurrentBranch(repository, log, remote, "refs/heads/featurE/feat-test");
        }

        private static IGitRepository MockRepository()
        {
            var repository = Substitute.For<IGitRepository>();
            return repository;
        }

        private static Remote MockRemote(IGitRepository repository)
        {
            var branches = new TestableBranchCollection();
            var tipId = new ObjectId("c6d8764d20ff16c0df14c73680e52b255b608926");
            var tip = new TestableCommit(tipId);
            var head = branches.Add("refs/heads/feature/feat-test", tip);
            var remote = new TesatbleRemote("origin");
            var references = new TestableReferenceCollection
            {
                {
                    "develop", "refs/heads/develop"
                }
            };

            repository.Refs.Returns(references);
            repository.Head.Returns(head);
            repository.Branches.Returns(branches);
            return remote;
        }

        private class TestableBranchCollection : BranchCollection
        {
            IDictionary<string, Branch> branches = new Dictionary<string, Branch>();

            public override Branch this[string name] =>
                branches.ContainsKey(name)
                    ? branches[name]
                    : null;

            public override Branch Add(string name, Commit commit)
            {
                var branch = new TestableBranch(name, commit);
                branches.Add(name, branch);
                return branch;
            }

            public override IEnumerator<Branch> GetEnumerator()
            {
                return branches.Values.GetEnumerator();
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

            public override string CanonicalName => canonicalName;
            public override Commit Tip => tip;
        }

        private class TestableCommit : Commit
        {
            private IObjectId id;

            public TestableCommit(IObjectId id)
            {
                this.id = id;
            }

            public override IObjectId Id => id;
        }

        private class TesatbleRemote : Remote
        {
            private string name;

            public TesatbleRemote(string name)
            {
                this.name = name;
            }

            public override string Name => name;
        }

        private class TestableReferenceCollection : ReferenceCollection
        {
            Reference reference;
            public override void Add(string name, string canonicalRefNameOrObjectish)
            {
                reference = new TestableReference(canonicalRefNameOrObjectish);
            }
            public override Reference UpdateTarget(Reference directRef, IObjectId targetId)
            {
                return reference;
            }
            public override Reference this[string name] => reference;
        }

        private class TestableReference : Reference
        {

            public TestableReference(string canonicalName)
            {
                this.CanonicalName = canonicalName;
            }

            public override string CanonicalName { get; }
        }
    }
}
