using GitVersion.Configuration;
using GitVersion.Testing.Extensions;
using LgCommit = LibGit2Sharp.Commit;
using LgSignature = LibGit2Sharp.Signature;
using LgTreeDefinition = LibGit2Sharp.TreeDefinition;

namespace GitVersion.Tests;

/// <summary>
/// Commits which share a committer timestamp are ordered by the revision walk through the shape of its priority
/// queue, which changes when commits are hidden. The derived commit log therefore only guarantees to return the
/// same commits, not the same order, for such histories. These tests pin that guarantee down.
/// </summary>
[TestFixture]
public class RepositoryStoreEqualTimestampTests : TestBase
{
    private static readonly IReadOnlySet<string> NothingExcluded = new HashSet<string>();

    /// <summary>
    /// Equal timestamps make the walk order ambiguous, but never the contents: the derived log has to hold
    /// exactly the commits the walk returns.
    /// </summary>
    [Test]
    public void DerivedCommitLogReturnsTheSameCommitsWhenTimestampsAreEqual()
    {
        for (var seed = 0; seed < 25; seed++)
        {
            using var fixture = new EmptyRepositoryFixture("main");
            var repository = CreateRandomDagWithEqualTimestamps(fixture, seed).ToGitRepository();

            var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, repository);
            var ignore = new IgnoreConfiguration();
            var head = repository.Head.Tip.ShouldNotBeNull();

            var history = sut.GetCommitLog(null, head, ignore);

            // Guards the generator: without merge commits these histories would not exercise the tie-breaking
            // this fixture is about.
            history.Count(commit => commit.Parents.Count > 1)
                .ShouldBeGreaterThan(0, $"seed {seed} produced a history without merge commits");

            foreach (var baseVersionSource in history)
            {
                var actual = sut.GetCommitLog(baseVersionSource, head, ignore, NothingExcluded)
                    .Select(element => element.Sha).OrderBy(element => element, StringComparer.Ordinal);
                var expected = sut.GetCommitLog(baseVersionSource, head, ignore)
                    .Select(element => element.Sha).OrderBy(element => element, StringComparer.Ordinal);

                actual.ShouldBe(
                    expected, $"seed {seed}, base version source '{baseVersionSource.Sha}'");
            }
        }
    }

    /// <summary>
    /// Where the order is ambiguous it still has to be a topological one, so a parent is never emitted before
    /// a child which is part of the same log.
    /// </summary>
    [Test]
    public void DerivedCommitLogIsAValidTopologicalOrderWhenTimestampsAreEqual()
    {
        for (var seed = 0; seed < 25; seed++)
        {
            using var fixture = new EmptyRepositoryFixture("main");
            var repository = CreateRandomDagWithEqualTimestamps(fixture, seed).ToGitRepository();

            var sut = new RepositoryStore(NullLogger<RepositoryStore>.Instance, repository);
            var ignore = new IgnoreConfiguration();
            var head = repository.Head.Tip.ShouldNotBeNull();

            foreach (var baseVersionSource in sut.GetCommitLog(null, head, ignore))
            {
                var log = sut.GetCommitLog(baseVersionSource, head, ignore, NothingExcluded);
                var positions = log
                    .Select((commit, index) => (commit.Sha, index))
                    .ToDictionary(element => element.Sha, element => element.index, StringComparer.Ordinal);

                // A parent must never be emitted before one of the children which are part of the same log.
                foreach (var commit in log)
                {
                    foreach (var parentSha in commit.Parents.Select(parent => parent.Sha).Where(positions.ContainsKey))
                    {
                        positions[parentSha].ShouldBeGreaterThan(
                            positions[commit.Sha],
                            $"seed {seed}: parent '{parentSha}' came before its child '{commit.Sha}'");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Builds a reproducible, randomly shaped history in which every commit carries the same timestamp, so the
    /// walk has to break ties between commits which are not ordered by time.
    /// </summary>
    private static LibGit2Sharp.IRepository CreateRandomDagWithEqualTimestamps(EmptyRepositoryFixture fixture, int seed)
    {
        var repository = fixture.Repository;

        // The histories have to be reproducible across runs and machines, so the shapes come from a small
        // deterministic generator rather than from an unpredictable source of randomness.
        var state = (uint)seed + 0x9E3779B9u;
        int Next(int exclusiveMaximum)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (int)(state % (uint)exclusiveMaximum);
        }

        var signature = new LgSignature("test", "test@test.io", DateTimeOffset.Now.AddHours(-1));
        var tree = repository.ObjectDatabase.CreateTree(new LgTreeDefinition());

        var created = new List<LgCommit>();
        for (var i = 0; i < 14; i++)
        {
            // The first commit has nothing to descend from, which Take handles without a special case.
            var parentCount = Math.Min(created.Count, 1 + Next(3));
            var parents = created.OrderBy(_ => Next(int.MaxValue)).Take(parentCount);

            created.Add(repository.ObjectDatabase.CreateCommit(
                signature, signature, $"commit {i}", tree, parents, prettifyMessage: false));
        }

        var tip = repository.ObjectDatabase.CreateCommit(
            signature, signature, "tip", tree, created, prettifyMessage: false);
        repository.Refs.Add("refs/heads/main", tip.Id, allowOverwrite: true);

        return repository;
    }
}
