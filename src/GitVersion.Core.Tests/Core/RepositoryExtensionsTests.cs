using GitVersion.Core.Tests.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace GitVersion.Core.Tests;

[TestFixture]
public class RepositoryExtensionsTests : TestBase
{
    [Test]
    public void EnsureLocalBranchExistsForCurrentBranch_CaseInsensitivelyMatchesBranches()
    {
        var repository = Substitute.For<IGitRepository>();
        var gitPreparer = Substitute.For<IGitPreparer>();
        var remote = MockRemote(repository);

        gitPreparer.EnsureLocalBranchExistsForCurrentBranch(remote, "refs/heads/featurE/feat-test");
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
