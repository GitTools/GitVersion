using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Testing.Extensions;

namespace GitVersion.Tests;

/// <summary>
/// Opens the same on-disk repository with both Git backends (libgit2 and managed) and
/// deep-compares every read surface: reference/branch/tag enumeration, order-sensitive
/// commit walks, merge bases over all commit pairs, diff paths and the uncommitted
/// changes count. This is the direct parity oracle demanded by the managed-git
/// migration plan, independent of which backend the test run selects globally.
/// </summary>
[TestFixture]
public class DualBackendParityTests : TestBase
{
    private static readonly CommitSortStrategies[] SortCombinations =
    [
        CommitSortStrategies.None,
        CommitSortStrategies.Time,
        CommitSortStrategies.Topological,
        CommitSortStrategies.Topological | CommitSortStrategies.Time,
        CommitSortStrategies.Time | CommitSortStrategies.Reverse,
        CommitSortStrategies.Topological | CommitSortStrategies.Time | CommitSortStrategies.Reverse
    ];

    [Test]
    public void ReferencesAreIdenticalOnBothBackends()
    {
        using var fixture = CreateComplexFixture();
        using var backends = OpenBothBackends(fixture);

        static IEnumerable<string> Describe(IGitRepository repository) =>
            repository.References.Select(r => $"{r.Name.Canonical} -> {r.TargetIdentifier} ({r.ReferenceTargetId?.Sha ?? "symbolic"})");

        Describe(backends.Managed).ShouldBe(Describe(backends.LibGit2));

        var libGit2Head = backends.LibGit2.References.Head.ShouldNotBeNull();
        var managedHead = backends.Managed.References.Head.ShouldNotBeNull();
        managedHead.Name.Canonical.ShouldBe(libGit2Head.Name.Canonical);
        managedHead.TargetIdentifier.ShouldBe(libGit2Head.TargetIdentifier);

        static IEnumerable<string> FromGlob(IGitRepository repository) =>
            repository.References.FromGlob("refs/heads/*").Select(r => $"{r.Name.Canonical} -> {r.TargetIdentifier}");

        FromGlob(backends.Managed).ShouldBe(FromGlob(backends.LibGit2));
    }

    [Test]
    public void BranchesAreIdenticalOnBothBackends()
    {
        using var fixture = CreateComplexFixture();
        using var backends = OpenBothBackends(fixture);

        static IEnumerable<string> Describe(IGitRepository repository) =>
            repository.Branches.Select(b => $"{b.Name.Canonical}@{b.Tip?.Sha} remote:{b.IsRemote} tracking:{b.IsTracking} detached:{b.IsDetachedHead}");

        Describe(backends.Managed).ShouldBe(Describe(backends.LibGit2));

        foreach (var name in new[] { "main", "develop", "refs/heads/develop", "feature/complex", "missing" })
        {
            var libGit2Branch = backends.LibGit2.Branches[name];
            var managedBranch = backends.Managed.Branches[name];
            (managedBranch?.Name.Canonical).ShouldBe(libGit2Branch?.Name.Canonical, $"lookup of '{name}'");
            (managedBranch?.Tip?.Sha).ShouldBe(libGit2Branch?.Tip?.Sha, $"tip of '{name}'");
        }

        backends.Managed.Head.Name.Canonical.ShouldBe(backends.LibGit2.Head.Name.Canonical);
        (backends.Managed.Head.Tip?.Sha).ShouldBe(backends.LibGit2.Head.Tip?.Sha);
        backends.Managed.IsHeadDetached.ShouldBe(backends.LibGit2.IsHeadDetached);
        backends.Managed.IsShallow.ShouldBe(backends.LibGit2.IsShallow);
    }

    [Test]
    public void TagsAreIdenticalOnBothBackends()
    {
        using var fixture = CreateComplexFixture();
        using var backends = OpenBothBackends(fixture);

        static IEnumerable<string> Describe(IGitRepository repository) =>
            repository.Tags.Select(t => $"{t.Name.Canonical} target:{t.TargetSha} commit:{t.Commit.Sha}");

        Describe(backends.Managed).ShouldBe(Describe(backends.LibGit2));
    }

    [Test]
    public void CommitLogsAreIdenticalOnBothBackends()
    {
        using var fixture = CreateComplexFixture();
        using var backends = OpenBothBackends(fixture);

        static IEnumerable<string> Describe(IGitRepository repository) =>
            repository.Commits.Select(c => $"{c.Sha} when:{c.When:O} merge:{c.IsMergeCommit} parents:{string.Join(',', c.Parents.Select(p => p.Sha))} message:{c.Message}");

        Describe(backends.Managed).ShouldBe(Describe(backends.LibGit2));

        foreach (var branchName in new[] { "main", "develop", "feature/complex" })
        {
            var libGit2Commits = backends.LibGit2.Branches[branchName].ShouldNotBeNull().Commits.Select(c => c.Sha);
            var managedCommits = backends.Managed.Branches[branchName].ShouldNotBeNull().Commits.Select(c => c.Sha);
            managedCommits.ShouldBe(libGit2Commits, $"commits of '{branchName}'");
        }
    }

    [Test]
    public void QueryBySequencesAreIdenticalOnBothBackends()
    {
        using var fixture = CreateCrissCrossFixture();
        using var backends = OpenBothBackends(fixture);

        var libGit2Commits = CollectAllCommits(backends.LibGit2);
        var managedCommits = CollectAllCommits(backends.Managed);
        managedCommits.Keys.Order().ShouldBe(libGit2Commits.Keys.Order());

        string?[] excludeCandidates = [null, .. libGit2Commits.Keys.Order().Take(3)];

        foreach (var sortBy in SortCombinations)
        {
            foreach (var firstParentOnly in new[] { false, true })
            {
                foreach (var excludeSha in excludeCandidates)
                {
                    var description = $"sort:{sortBy} firstParent:{firstParentOnly} exclude:{excludeSha ?? "none"}";

                    var libGit2Result = backends.LibGit2.Commits.QueryBy(new CommitFilter
                    {
                        IncludeReachableFrom = backends.LibGit2.Branches["main"],
                        ExcludeReachableFrom = excludeSha is null ? null : libGit2Commits[excludeSha],
                        SortBy = sortBy,
                        FirstParentOnly = firstParentOnly
                    }).Select(c => c.Sha);

                    var managedResult = backends.Managed.Commits.QueryBy(new CommitFilter
                    {
                        IncludeReachableFrom = backends.Managed.Branches["main"],
                        ExcludeReachableFrom = excludeSha is null ? null : managedCommits[excludeSha],
                        SortBy = sortBy,
                        FirstParentOnly = firstParentOnly
                    }).Select(c => c.Sha);

                    managedResult.ShouldBe(libGit2Result, description);
                }
            }
        }
    }

    [Test]
    public void MergeBasesAreIdenticalOnBothBackendsForAllCommitPairs()
    {
        using var fixture = CreateCrissCrossFixture();
        using var backends = OpenBothBackends(fixture);

        var libGit2Commits = CollectAllCommits(backends.LibGit2);
        var managedCommits = CollectAllCommits(backends.Managed);
        var shas = libGit2Commits.Keys.Order().ToList();

        foreach (var first in shas)
        {
            foreach (var second in shas)
            {
                var libGit2MergeBase = backends.LibGit2.FindMergeBase(libGit2Commits[first], libGit2Commits[second]);
                var managedMergeBase = backends.Managed.FindMergeBase(managedCommits[first], managedCommits[second]);
                (managedMergeBase?.Sha).ShouldBe(libGit2MergeBase?.Sha, $"merge base of {first[..7]} and {second[..7]}");
            }
        }
    }

    [Test]
    public void DiffPathsAreIdenticalOnBothBackendsForAllCommits()
    {
        using var fixture = CreateComplexFixture();
        using var backends = OpenBothBackends(fixture);

        var libGit2Commits = CollectAllCommits(backends.LibGit2);
        var managedCommits = CollectAllCommits(backends.Managed);

        foreach (var sha in libGit2Commits.Keys.Order())
        {
            managedCommits[sha].DiffPaths.ShouldBe(libGit2Commits[sha].DiffPaths, $"diff paths of {sha[..7]}");
        }
    }

    [Test]
    public void UncommittedChangesCountIsIdenticalOnBothBackends()
    {
        using var fixture = new EmptyRepositoryFixture();
        var signature = new LibGit2Sharp.Signature("unit test", "test@example.com", DateTimeOffset.Now);

        var trackedFile = FileSystemHelper.Path.Combine(fixture.RepositoryPath, "tracked.txt");
        File.WriteAllText(trackedFile, "one");
        LibGit2Sharp.Commands.Stage(fixture.Repository, "tracked.txt");
        fixture.Repository.Commit("add tracked file", signature, signature, new LibGit2Sharp.CommitOptions());

        File.WriteAllText(trackedFile, "two");
        File.WriteAllText(FileSystemHelper.Path.Combine(fixture.RepositoryPath, "untracked.txt"), "new");
        File.WriteAllText(FileSystemHelper.Path.Combine(fixture.RepositoryPath, "staged.txt"), "staged");
        LibGit2Sharp.Commands.Stage(fixture.Repository, "staged.txt");

        using var backends = OpenBothBackends(fixture);
        backends.Managed.UncommittedChangesCount().ShouldBe(backends.LibGit2.UncommittedChangesCount());
    }

    [Test]
    public void EmptyRepositoryBehavesIdenticallyOnBothBackends()
    {
        using var fixture = new EmptyRepositoryFixture();
        File.WriteAllText(FileSystemHelper.Path.Combine(fixture.RepositoryPath, "untracked.txt"), "new");

        using var backends = OpenBothBackends(fixture);

        backends.Managed.Head.Name.Canonical.ShouldBe(backends.LibGit2.Head.Name.Canonical);
        backends.Managed.Head.Tip.ShouldBe(backends.LibGit2.Head.Tip);
        backends.Managed.IsHeadDetached.ShouldBe(backends.LibGit2.IsHeadDetached);
        backends.Managed.UncommittedChangesCount().ShouldBe(backends.LibGit2.UncommittedChangesCount());
    }

    [Test]
    public void DetachedHeadBehavesIdenticallyOnBothBackends()
    {
        using var fixture = CreateComplexFixture();
        var detachAt = fixture.Repository.Head.Tip.Parents.First();
        LibGit2Sharp.Commands.Checkout(fixture.Repository, detachAt);

        using var backends = OpenBothBackends(fixture);

        backends.Managed.IsHeadDetached.ShouldBeTrue();
        backends.Managed.IsHeadDetached.ShouldBe(backends.LibGit2.IsHeadDetached);
        backends.Managed.Head.Name.Canonical.ShouldBe(backends.LibGit2.Head.Name.Canonical);
        backends.Managed.Head.IsDetachedHead.ShouldBe(backends.LibGit2.Head.IsDetachedHead);
        (backends.Managed.Head.Tip?.Sha).ShouldBe(backends.LibGit2.Head.Tip?.Sha);
    }

    [Test]
    public void RemotesAndRemoteBranchesAreIdenticalOnBothBackends()
    {
        using var fixture = new RemoteRepositoryFixture();
        using var backends = OpenBothBackends(fixture.LocalRepositoryFixture);

        static IEnumerable<string> DescribeRemotes(IGitRepository repository) =>
            repository.Remotes.Select(r =>
                $"{r.Name} {r.Url} fetch:[{string.Join(';', r.FetchRefSpecs.Select(s => s.Specification))}] push:[{string.Join(';', r.PushRefSpecs.Select(s => s.Specification))}]");

        DescribeRemotes(backends.Managed).ShouldBe(DescribeRemotes(backends.LibGit2));

        var libGit2Remote = backends.LibGit2.Remotes["origin"].ShouldNotBeNull();
        var managedRemote = backends.Managed.Remotes["origin"].ShouldNotBeNull();
        managedRemote.Url.ShouldBe(libGit2Remote.Url);

        foreach (var (managedSpec, libGit2Spec) in managedRemote.FetchRefSpecs.Zip(libGit2Remote.FetchRefSpecs))
        {
            managedSpec.Specification.ShouldBe(libGit2Spec.Specification);
            managedSpec.Direction.ShouldBe(libGit2Spec.Direction);
            managedSpec.Source.ShouldBe(libGit2Spec.Source);
            managedSpec.Destination.ShouldBe(libGit2Spec.Destination);
        }

        static IEnumerable<string> DescribeBranches(IGitRepository repository) =>
            repository.Branches.Select(b => $"{b.Name.Canonical}@{b.Tip?.Sha} remote:{b.IsRemote} tracking:{b.IsTracking}");

        DescribeBranches(backends.Managed).ShouldBe(DescribeBranches(backends.LibGit2));
    }

    [Test]
    public void RepositoryPathsAreIdenticalOnBothBackends()
    {
        using var fixture = CreateComplexFixture();
        using var backends = OpenBothBackends(fixture);

        backends.Managed.Path.ShouldBe(backends.LibGit2.Path);
        backends.Managed.WorkingDirectory.ShouldBe(backends.LibGit2.WorkingDirectory);
    }

    [Test]
    public void OctopusMergeBehavesIdenticallyOnBothBackends()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("base");
        fixture.BranchTo("topic/one");
        fixture.MakeACommit("topic one");
        fixture.Checkout("main");
        fixture.BranchTo("topic/two");
        fixture.MakeACommit("topic two");
        fixture.Checkout("main");
        fixture.MakeACommit("main one");
        GitTestExtensions.ExecuteGitCmd($"-C {fixture.RepositoryPath} -c user.name=test -c user.email=test@example.com merge topic/one topic/two -m octopus", ".");

        using var backends = OpenBothBackends(fixture);

        backends.Managed.Head.Tip.ShouldNotBeNull().Parents.Count.ShouldBe(3);
        AssertCommitLogParity(backends);
        AssertMergeBaseParity(backends);
        AssertDiffPathsParity(backends);
    }

    [Test]
    public void PackedAndLooseReferencesBehaveIdenticallyOnBothBackends()
    {
        using var fixture = CreateComplexFixture();

        // Pack everything, then shadow one packed ref with a loose one and add brand-new loose refs,
        // so both stores must merge packed and loose entries (and their peeled annotated-tag lines).
        GitTestExtensions.ExecuteGitCmd($"-C {fixture.RepositoryPath} pack-refs --all", ".");
        fixture.MakeACommit("shadows the packed main entry");
        fixture.BranchTo("loose/branch");
        fixture.ApplyTag("loose-tag");

        using var backends = OpenBothBackends(fixture);

        static IEnumerable<string> DescribeReferences(IGitRepository repository) =>
            repository.References.Select(r => $"{r.Name.Canonical} -> {r.TargetIdentifier}");

        DescribeReferences(backends.Managed).ShouldBe(DescribeReferences(backends.LibGit2));

        static IEnumerable<string> DescribeTags(IGitRepository repository) =>
            repository.Tags.Select(t => $"{t.Name.Canonical} target:{t.TargetSha} commit:{t.Commit.Sha}");

        DescribeTags(backends.Managed).ShouldBe(DescribeTags(backends.LibGit2));

        static IEnumerable<string> DescribeBranches(IGitRepository repository) =>
            repository.Branches.Select(b => $"{b.Name.Canonical}@{b.Tip?.Sha}");

        DescribeBranches(backends.Managed).ShouldBe(DescribeBranches(backends.LibGit2));
    }

    [Test]
    public void LightweightAndAnnotatedTagOnTheSameCommitBehaveIdenticallyOnBothBackends()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("only commit");
        fixture.ApplyTag("lightweight");
        fixture.Repository.Tags.Add("annotated", fixture.Repository.Head.Tip, new LibGit2Sharp.Signature("unit test", "test@example.com", DateTimeOffset.Now), "the same commit, annotated");

        using var backends = OpenBothBackends(fixture);

        static IEnumerable<string> Describe(IGitRepository repository) =>
            repository.Tags.Select(t => $"{t.Name.Canonical} target:{t.TargetSha} commit:{t.Commit.Sha}");

        Describe(backends.Managed).ShouldBe(Describe(backends.LibGit2));
    }

    [Test]
    public void ShallowCloneBehavesIdenticallyOnBothBackends()
    {
        using var fixture = new RemoteRepositoryFixture();
        using var localFixture = fixture.CloneRepository();
        localFixture.MakeShallow();

        using var backends = OpenBothBackends(localFixture);

        backends.Managed.IsShallow.ShouldBeTrue();
        backends.Managed.IsShallow.ShouldBe(backends.LibGit2.IsShallow);
        AssertCommitLogParity(backends);
    }

    [Test]
    public void WorktreeBehavesIdenticallyOnBothBackends()
    {
        using var fixture = CreateComplexFixture();
        var worktreePath = FileSystemHelper.Path.GetRepositoryTempPath();
        GitTestExtensions.ExecuteGitCmd($"-C {fixture.RepositoryPath} worktree add {worktreePath} develop", ".");

        try
        {
            using var backends = OpenBothBackendsAt(worktreePath);

            backends.Managed.Path.ShouldBe(backends.LibGit2.Path);
            backends.Managed.WorkingDirectory.ShouldBe(backends.LibGit2.WorkingDirectory);
            backends.Managed.Head.Name.Canonical.ShouldBe(backends.LibGit2.Head.Name.Canonical);
            (backends.Managed.Head.Tip?.Sha).ShouldBe(backends.LibGit2.Head.Tip?.Sha);
            backends.Managed.UncommittedChangesCount().ShouldBe(backends.LibGit2.UncommittedChangesCount());

            static IEnumerable<string> DescribeReferences(IGitRepository repository) =>
                repository.References.Select(r => $"{r.Name.Canonical} -> {r.TargetIdentifier}");

            DescribeReferences(backends.Managed).ShouldBe(DescribeReferences(backends.LibGit2));
            AssertCommitLogParity(backends);
        }
        finally
        {
            GitTestExtensions.ExecuteGitCmd($"-C {fixture.RepositoryPath} worktree remove --force {worktreePath}", ".");
        }
    }

    [Test]
    public void IndexVersionFourBehavesIdenticallyOnBothBackends()
    {
        using var fixture = new EmptyRepositoryFixture();
        var signature = new LibGit2Sharp.Signature("unit test", "test@example.com", DateTimeOffset.Now);

        var trackedFile = FileSystemHelper.Path.Combine(fixture.RepositoryPath, "tracked.txt");
        File.WriteAllText(trackedFile, "one");
        LibGit2Sharp.Commands.Stage(fixture.Repository, "tracked.txt");
        fixture.Repository.Commit("add tracked file", signature, signature, new LibGit2Sharp.CommitOptions());

        GitTestExtensions.ExecuteGitCmd($"-C {fixture.RepositoryPath} update-index --index-version 4", ".");
        File.WriteAllText(trackedFile, "two");
        File.WriteAllText(FileSystemHelper.Path.Combine(fixture.RepositoryPath, "untracked.txt"), "new");

        using var backends = OpenBothBackends(fixture);
        backends.Managed.UncommittedChangesCount().ShouldBe(backends.LibGit2.UncommittedChangesCount());
    }

    private static void AssertCommitLogParity(BackendPair backends)
    {
        static IEnumerable<string> Describe(IGitRepository repository) =>
            repository.Commits.Select(c => $"{c.Sha} merge:{c.IsMergeCommit} parents:{string.Join(',', c.Parents.Select(p => p.Sha))}");

        Describe(backends.Managed).ShouldBe(Describe(backends.LibGit2));
    }

    private static void AssertMergeBaseParity(BackendPair backends)
    {
        var libGit2Commits = CollectAllCommits(backends.LibGit2);
        var managedCommits = CollectAllCommits(backends.Managed);

        foreach (var first in libGit2Commits.Keys.Order())
        {
            foreach (var second in libGit2Commits.Keys.Order())
            {
                var libGit2MergeBase = backends.LibGit2.FindMergeBase(libGit2Commits[first], libGit2Commits[second]);
                var managedMergeBase = backends.Managed.FindMergeBase(managedCommits[first], managedCommits[second]);
                (managedMergeBase?.Sha).ShouldBe(libGit2MergeBase?.Sha, $"merge base of {first[..7]} and {second[..7]}");
            }
        }
    }

    private static void AssertDiffPathsParity(BackendPair backends)
    {
        var libGit2Commits = CollectAllCommits(backends.LibGit2);
        var managedCommits = CollectAllCommits(backends.Managed);

        foreach (var sha in libGit2Commits.Keys.Order())
        {
            managedCommits[sha].DiffPaths.ShouldBe(libGit2Commits[sha].DiffPaths, $"diff paths of {sha[..7]}");
        }
    }

    private static RepositoryFixtureBase CreateComplexFixture()
    {
        var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial commit");
        fixture.ApplyTag("1.0.0");
        fixture.BranchTo("develop");
        fixture.MakeACommit("develop one");
        fixture.Checkout("main");
        fixture.MakeACommit("main two");
        fixture.MergeNoFF("develop");
        fixture.Repository.Tags.Add("v2.0.0", fixture.Repository.Head.Tip, new LibGit2Sharp.Signature("unit test", "test@example.com", DateTimeOffset.Now), "an annotated tag");
        fixture.BranchTo("feature/complex");
        fixture.MakeACommit("feature one");
        fixture.Checkout("develop");
        fixture.MakeACommit("develop two");
        fixture.Checkout("main");
        return fixture;
    }

    /// <summary>
    /// Builds a criss-cross history (two merge commits whose parents are swapped), which is
    /// the classic ambiguous merge-base scenario, plus commits with equal timestamps.
    /// </summary>
    private static RepositoryFixtureBase CreateCrissCrossFixture()
    {
        var fixture = new EmptyRepositoryFixture();
        var signature = new LibGit2Sharp.Signature("unit test", "test@example.com", DateTimeOffset.Now);
        var mergeOptions = new LibGit2Sharp.MergeOptions { FastForwardStrategy = LibGit2Sharp.FastForwardStrategy.NoFastForward };

        fixture.MakeACommit("base");
        fixture.BranchTo("develop");
        fixture.MakeACommit("develop one");
        var developHead = fixture.Repository.Head.Tip;
        fixture.Checkout("main");
        fixture.MakeACommit("main one");
        var mainHead = fixture.Repository.Head.Tip;

        // main merges develop's old head while develop merges main's old head: criss-cross.
        fixture.Repository.Merge(developHead, signature, mergeOptions);
        fixture.Checkout("develop");
        fixture.Repository.Merge(mainHead, signature, mergeOptions);

        fixture.MakeACommit("develop two");
        fixture.Checkout("main");
        fixture.MakeACommit("main two");
        return fixture;
    }

    private static BackendPair OpenBothBackends(RepositoryFixtureBase fixture) => OpenBothBackendsAt(fixture.RepositoryPath);

    private static BackendPair OpenBothBackendsAt(string repositoryPath)
    {
        var libGit2 = new GitRepository(NullLogger<GitRepository>.Instance);
        libGit2.DiscoverRepository(repositoryPath);

        var cliMutator = new GitCliMutator(NullLogger<GitCliMutator>.Instance, new GitCliExecutor(NullLogger<GitCliExecutor>.Instance));
        var managed = new ManagedGitRepository(NullLogger<ManagedGitRepository>.Instance, cliMutator);
        managed.DiscoverRepository(repositoryPath);

        return new(libGit2, managed);
    }

    private static Dictionary<string, ICommit> CollectAllCommits(IGitRepository repository)
    {
        var commits = new Dictionary<string, ICommit>(StringComparer.Ordinal);

        foreach (var branch in repository.Branches)
        {
            foreach (var commit in branch.Commits)
            {
                commits[commit.Sha] = commit;
            }
        }

        return commits;
    }

    private sealed record BackendPair(IGitRepository LibGit2, IGitRepository Managed) : IDisposable
    {
        public void Dispose()
        {
            LibGit2.Dispose();
            Managed.Dispose();
        }
    }
}
