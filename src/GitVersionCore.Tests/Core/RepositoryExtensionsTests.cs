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
        private static void EnsureLocalBranchExistsForCurrentBranch(IGitRepository repo, ILog log, IRemote remote, string currentBranch)
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

            if (repo.Branches.All(b => !b.Name.Canonical.IsEquivalentTo(localCanonicalName)))
            {
                log.Info(isBranch ? $"Creating local branch {localCanonicalName}"
                    : $"Creating local branch {localCanonicalName} pointing at {repoTipId}");
                repo.Refs.Add(localCanonicalName, repoTipId.Sha);
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
            var repository = Substitute.For<IGitRepository>();
            var remote = MockRemote(repository);

            EnsureLocalBranchExistsForCurrentBranch(repository, log, remote, "refs/heads/featurE/feat-test");
        }

        private static IRemote MockRemote(IGitRepository repository)
        {
            var tipId = Substitute.For<IObjectId>();
            tipId.Sha.Returns("c6d8764d20ff16c0df14c73680e52b255b608926");

            var tip = Substitute.For<ICommit>();
            tip.Id.Returns(tipId);
            tip.Sha.Returns(tipId.Sha);

            var remote = Substitute.For<IRemote>();
            remote.Name.Returns("origin");

            var branch = Substitute.For<IBranch>();
            branch.Tip.Returns(tip);
            branch.Name.Returns(new ReferenceName("refs/heads/feature/feat-test"));

            var branches = Substitute.For<IBranchCollection>();
            branches[branch.Name.Canonical].Returns(branch);
            branches.GetEnumerator().Returns(_ => ((IEnumerable<IBranch>)new[] { branch }).GetEnumerator());

            var reference = Substitute.For<IReference>();
            reference.Name.Returns(new ReferenceName("refs/heads/develop"));

            var references = Substitute.For<IReferenceCollection>();
            references["develop"].Returns(reference);
            references.GetEnumerator().Returns(_ => ((IEnumerable<IReference>)new[] { reference }).GetEnumerator());

            repository.Refs.Returns(references);
            repository.Head.Returns(branch);
            repository.Branches.Returns(branches);
            return remote;
        }
    }
}
