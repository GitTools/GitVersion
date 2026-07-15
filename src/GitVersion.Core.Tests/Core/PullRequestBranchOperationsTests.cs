using GitVersion.Git;

namespace GitVersion.Tests;

[TestFixture]
public class PullRequestBranchOperationsTests : TestBase
{
    private const string HeadTipSha = "0123456789012345678901234567890123456789";

    [Test]
    public void HeadAdvertisementIsNotACandidateTip()
    {
        // libgit2's remote listing resolves the HEAD symref into an extra entry pointing
        // at the default branch tip; it must not count against the single-match check.
        var repository = CreateRepository();

        PullRequestBranchOperations.CreateBranchForPullRequestBranch(
            repository,
            NullLogger.Instance,
            _ => [new("HEAD", HeadTipSha), new("refs/pull/2/merge", HeadTipSha)]);

        repository.References.Received().Add("refs/heads/pull/2/merge", HeadTipSha);
        repository.Received().Checkout("refs/heads/pull/2/merge");
    }

    [Test]
    public void PeeledAndDuplicateEntriesAreNotCandidateTips()
    {
        const string tagObjectSha = "1111111111111111111111111111111111111111";
        const string otherCommitSha = "2222222222222222222222222222222222222222";
        var repository = CreateRepository();

        PullRequestBranchOperations.CreateBranchForPullRequestBranch(
            repository,
            NullLogger.Instance,
            _ =>
            [
                new("refs/pull/2/merge", HeadTipSha),
                new("refs/pull/2/merge", HeadTipSha),      // duplicate (e.g. a resolved symref)
                new("refs/tags/v1.0.0", tagObjectSha),     // annotated tag pointing elsewhere
                new("refs/tags/v1.0.0^{}", otherCommitSha) // its peeled entry
            ]);

        repository.References.Received().Add("refs/heads/pull/2/merge", HeadTipSha);
    }

    [Test]
    public void AnnotatedTagIsMatchedThroughItsPeeledEntry()
    {
        // A detached HEAD at an annotated tag's commit: the tag ref itself carries the
        // tag-object sha, so only the peeled "^{}" entry points at the head tip. It must
        // fold into the base tag ref and take the tag checkout path.
        const string tagObjectSha = "1111111111111111111111111111111111111111";
        var repository = CreateRepository();

        PullRequestBranchOperations.CreateBranchForPullRequestBranch(
            repository,
            NullLogger.Instance,
            _ =>
            [
                new("refs/tags/v1.0.0", tagObjectSha),
                new("refs/tags/v1.0.0^{}", HeadTipSha)
            ]);

        repository.Received().Checkout(HeadTipSha);
    }

    [Test]
    public void MultipleDistinctCandidateTipsStillFail()
    {
        var repository = CreateRepository();

        Should.Throw<WarningException>(() => PullRequestBranchOperations.CreateBranchForPullRequestBranch(
                repository,
                NullLogger.Instance,
                _ => [new("refs/pull/2/merge", HeadTipSha), new("refs/heads/main", HeadTipSha)]))
            .Message.ShouldContain("more than one remote tip");
    }

    private static IMutatingGitRepository CreateRepository()
    {
        var tip = Substitute.For<ICommit>();
        tip.Sha.Returns(HeadTipSha);

        var head = Substitute.For<IBranch>();
        head.Tip.Returns(tip);

        var remote = Substitute.For<IRemote>();
        remote.Name.Returns("origin");
        remote.Url.Returns("https://example.com/repo.git");

        var remotes = Substitute.For<IRemoteCollection>();
        remotes.GetEnumerator().Returns(_ => new List<IRemote> { remote }.GetEnumerator());

        var repository = Substitute.For<IMutatingGitRepository>();
        repository.Head.Returns(head);
        repository.Remotes.Returns(remotes);
        return repository;
    }
}
