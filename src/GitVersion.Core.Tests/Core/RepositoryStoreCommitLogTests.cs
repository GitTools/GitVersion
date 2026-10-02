using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Testing.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Tests;

/// <summary>
/// The commit log with excluded commits is derived from a single cached revision walk instead of asking git for
/// one walk per base version source. These tests pin that optimisation to the behaviour of the walks it replaces.
/// </summary>
[TestFixture]
public class RepositoryStoreCommitLogTests : TestBase
{
    private static readonly IReadOnlySet<string> NothingExcluded = new HashSet<string>();

    /// <summary>
    /// The derived log has to be indistinguishable from a walk which hides the base version source, for every
    /// commit of a branched and merged history.
    /// </summary>
    [Test]
    public void DerivedCommitLogMatchesRevisionWalkForEveryBaseVersionSource()
    {
        using var fixture = CreateBranchedAndMergedRepository();
        var repository = fixture.Repository.ToGitRepository();
        var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, repository);
        var ignore = new IgnoreConfiguration();

        var head = repository.Head.Tip.ShouldNotBeNull();

        foreach (var baseVersionSource in AllCommits(repository))
        {
            var actual = sut.GetCommitLog(baseVersionSource, head, ignore, NothingExcluded).Select(element => element.Sha);
            var expected = RevisionWalk(repository, baseVersionSource, head).Select(element => element.Sha);

            actual.ShouldBe(expected, $"commit log for base version source '{baseVersionSource.Sha}'");
        }
    }

    /// <summary>
    /// Tags can point at commits which the head cannot reach. The graph has to fall back to the real commit
    /// graph until the traversal re-enters it, so that the reachable ancestors are still removed.
    /// </summary>
    [Test]
    public void DerivedCommitLogMatchesRevisionWalkWhenBaseVersionSourceIsUnreachableFromHead()
    {
        using var fixture = CreateBranchedAndMergedRepository();

        // 'feature/unmerged' is never merged back, so its tip is not reachable from the head of 'main' while
        // some of its ancestors still are.
        fixture.Checkout("main");
        fixture.BranchTo("feature/unmerged");
        fixture.MakeACommit("unmerged one");
        fixture.MakeACommit("unmerged two");
        var repository = fixture.Repository.ToGitRepository();
        var unmergedTip = repository.FindBranch("feature/unmerged").ShouldNotBeNull().Tip.ShouldNotBeNull();

        fixture.Checkout("main");
        fixture.MakeACommit("main after branching off");

        repository = fixture.Repository.ToGitRepository();
        var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, repository);
        var ignore = new IgnoreConfiguration();
        var head = repository.Head.Tip.ShouldNotBeNull();

        var actual = sut.GetCommitLog(unmergedTip, head, ignore, NothingExcluded).Select(element => element.Sha);
        var expected = RevisionWalk(repository, unmergedTip, head).Select(element => element.Sha);

        actual.ShouldBe(expected);
    }

    /// <summary>
    /// Excluding a commit has to remove exactly what hiding it in a revision walk removes: the commit itself
    /// and everything it builds upon.
    /// </summary>
    [Test]
    public void DerivedCommitLogExcludesTheAncestorsOfTheGivenShas()
    {
        using var fixture = CreateBranchedAndMergedRepository();
        var repository = fixture.Repository.ToGitRepository();
        var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, repository);
        var ignore = new IgnoreConfiguration();
        var head = repository.Head.Tip.ShouldNotBeNull();

        // Excluding a commit must remove exactly that commit and everything it builds upon, which is the same
        // set a revision walk hides when the commit is used as the base version source.
        foreach (var excluded in sut.GetCommitLog(null, head, ignore))
        {
            var actual = sut.GetCommitLog(null, head, ignore, new HashSet<string> { excluded.Sha })
                .Select(element => element.Sha);
            var expected = RevisionWalk(repository, excluded, head).Select(element => element.Sha);

            actual.ShouldBe(expected, $"commit log excluding '{excluded.Sha}'");
        }
    }

    /// <summary>
    /// The tagged ancestor walk stops at ignored commits, which is what the dictionary based pruning it
    /// replaced did, so an ignored commit shields everything below it from that exclusion.
    /// </summary>
    [Test]
    public void DerivedCommitLogStopsExcludingAtIgnoredCommits()
    {
        using var fixture = CreateBranchedAndMergedRepository();
        var repository = fixture.Repository.ToGitRepository();
        var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, repository);
        var head = repository.Head.Tip.ShouldNotBeNull();

        var history = sut.GetCommitLog(null, head, new IgnoreConfiguration());

        // The oldest commit is an ancestor of every other one, so ignoring the commit right above it shields it
        // from the exclusion walk, exactly as the dictionary based pruning this replaced did.
        var root = history[^1];
        var shield = history[^2];
        var ignore = new IgnoreConfiguration { Shas = new HashSet<string> { shield.Sha } };

        var actual = sut.GetCommitLog(null, head, ignore, new HashSet<string> { history[0].Sha });

        actual.Select(element => element.Sha).ShouldContain(root.Sha);
    }

    /// <summary>Ignored commits must not appear in the result, even though the graph still contains them.</summary>
    [Test]
    public void DerivedCommitLogHonoursIgnoredCommits()
    {
        using var fixture = CreateBranchedAndMergedRepository();
        var repository = fixture.Repository.ToGitRepository();
        var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, repository);
        var head = repository.Head.Tip.ShouldNotBeNull();

        var ignoredSha = sut.GetCommitLog(null, head, new IgnoreConfiguration())
            .Select(element => element.Sha).Last();

        var actual = sut
            .GetCommitLog(null, head, new IgnoreConfiguration { Shas = new HashSet<string> { ignoredSha } }, NothingExcluded)
            .Select(element => element.Sha);

        actual.ShouldNotContain(ignoredSha);
    }

    /// <summary>
    /// Ignoring a commit must not sever the parent links which the ancestor walk follows. Checked for every
    /// combination of ignored commit and base version source, because an earlier revision of this change
    /// built the graph from the filtered walk and silently kept ancestors that should have been removed.
    /// </summary>
    [Test]
    public void DerivedCommitLogMatchesRevisionWalkWhenACommitInTheMiddleOfTheHistoryIsIgnored()
    {
        using var fixture = CreateBranchedAndMergedRepository();
        var repository = fixture.Repository.ToGitRepository();
        var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, repository);
        var head = repository.Head.Tip.ShouldNotBeNull();

        var allCommits = sut.GetCommitLog(null, head, new IgnoreConfiguration());

        // Ignoring a commit must not sever the parent links of the commits which build upon it: the ancestors
        // of a base version source have to be removed even when an ignored commit sits between the two.
        foreach (var ignoredSha in allCommits.Select(element => element.Sha))
        {
            var ignore = new IgnoreConfiguration { Shas = new HashSet<string> { ignoredSha } };

            foreach (var baseVersionSource in allCommits)
            {
                var actual = sut.GetCommitLog(baseVersionSource, head, ignore, NothingExcluded)
                    .Select(element => element.Sha);
                var expected = RevisionWalk(repository, baseVersionSource, head)
                    .Where(element => element.Sha != ignoredSha)
                    .Select(element => element.Sha);

                actual.ShouldBe(
                    expected, $"base version source '{baseVersionSource.Sha}' while ignoring '{ignoredSha}'");
            }
        }
    }

    /// <summary>
    /// The whole point of deriving the log is that the history is walked once, no matter how many base version
    /// sources and excluded commits are asked about afterwards.
    /// </summary>
    [Test]
    public void DerivedCommitLogPerformsExactlyOneRevisionWalk()
    {
        using var fixture = CreateBranchedAndMergedRepository();
        var repository = fixture.Repository.ToGitRepository();
        var commits = new CountingCommitCollection(repository.Commits);
        var countingRepository = Substitute.For<IGitRepository>();
        countingRepository.Commits.Returns(commits);

        var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, countingRepository);
        var ignore = new IgnoreConfiguration();
        var head = repository.Head.Tip.ShouldNotBeNull();

        // The scan for a commit resetting the version bump reads the full history with nothing excluded, so it
        // must not cost a walk of its own either.
        sut.GetCommitLog(null, head, ignore, NothingExcluded);

        foreach (var baseVersionSource in AllCommits(repository))
        {
            sut.GetCommitLog(baseVersionSource, head, ignore, new HashSet<string> { baseVersionSource.Sha });
        }

        commits.QueryCount.ShouldBe(1);
    }

    /// <summary>
    /// The scan for a commit resetting the version bump reads the whole history. It must go through the cached
    /// graph as well, or the common path where nothing resets the bump costs two walks instead of one.
    /// </summary>
    [Test]
    public void IncrementStrategyFinderReadsTheWholeHistoryWithASingleRevisionWalk()
    {
        using var fixture = CreateBranchedAndMergedRepository();
        var repository = fixture.Repository.ToGitRepository();
        var commits = new CountingCommitCollection(repository.Commits);
        var countingRepository = Substitute.For<IGitRepository>();
        countingRepository.Commits.Returns(commits);
        countingRepository.Tags.Returns(repository.Tags);

        var store = new RepositoryStore(NullLogger<RepositoryStore>.Instance, countingRepository);

        // The real repository is used rather than a stub: the fixture carries no tags, so it yields no tagged
        // versions anyway, and it reads them from the tag collection rather than through a revision walk.
        var taggedSemanticVersionRepository = new TaggedSemanticVersionRepository(
            NullLogger<TaggedSemanticVersionRepository>.Instance, store);
        var sut = new IncrementStrategyFinder(store, taggedSemanticVersionRepository);

        // The configuration has to be resolved for a concrete branch: the branchless effective configuration
        // leaves the increment at 'Inherit', which has no version field to increment.
        var configuration = GitFlowConfigurationBuilder.New.Build()
            .GetEffectiveConfiguration(ReferenceName.FromBranchName("main"));
        var head = repository.Head.Tip.ShouldNotBeNull();

        foreach (var baseVersionSource in AllCommits(repository))
        {
            sut.DetermineIncrementedField(head, baseVersionSource, shouldIncrement: true, configuration, label: null);
        }

        commits.QueryCount.ShouldBe(1);
    }

    /// <summary>Builds a history with a branch, a merge into it and a merge back, so the graph is not a chain.</summary>
    private static EmptyRepositoryFixture CreateBranchedAndMergedRepository()
    {
        var fixture = new EmptyRepositoryFixture("main");

        fixture.MakeACommit("initial");
        fixture.MakeACommit("main one");

        fixture.BranchTo("develop");
        fixture.MakeACommit("develop one");
        fixture.MakeACommit("develop two");

        fixture.Checkout("main");
        fixture.MakeACommit("main two");

        fixture.BranchTo("feature/a");
        fixture.MakeACommit("feature a one");

        fixture.Checkout("develop");
        fixture.MergeNoFF("feature/a");
        fixture.MakeACommit("develop three");

        fixture.Checkout("main");
        fixture.MergeNoFF("develop");
        fixture.MakeACommit("main three");

        return fixture;
    }

    /// <summary>Every commit reachable from the head of the repository.</summary>
    private static IEnumerable<ICommit> AllCommits(IGitRepository repository)
        => repository.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = repository.Head.Tip });

    /// <summary>The revision walk the optimisation replaces, used as the reference result.</summary>
    private static IEnumerable<ICommit> RevisionWalk(IGitRepository repository, ICommit? baseVersionSource, ICommit head)
        => repository.Commits.QueryBy(new CommitFilter
        {
            IncludeReachableFrom = head,
            ExcludeReachableFrom = baseVersionSource,
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
        });

    /// <summary>Counts how often the repository is asked for a revision walk.</summary>
    private sealed class CountingCommitCollection(ICommitCollection inner) : ICommitCollection
    {
        public int QueryCount { get; private set; }

        public IEnumerable<ICommit> GetCommitsPriorTo(DateTimeOffset olderThan) => inner.GetCommitsPriorTo(olderThan);

        public IEnumerable<ICommit> QueryBy(CommitFilter commitFilter)
        {
            QueryCount++;
            return inner.QueryBy(commitFilter);
        }

        public IEnumerator<ICommit> GetEnumerator() => inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
