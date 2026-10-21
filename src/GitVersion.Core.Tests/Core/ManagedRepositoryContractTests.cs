using System.IO.Abstractions;
using GitVersion.Git;
using GitVersion.Helpers;
using GitVersion.Testing.Extensions;
using FixtureGit = GitVersion.Testing.Git;

namespace GitVersion.Tests;

/// <summary>Retains the migration corpus using Git CLI or explicit scenario expectations.</summary>
[TestFixture]
public class ManagedRepositoryContractTests : TestBase
{
    [Test]
    public void ReferencesMatchGitCli()
    {
        using var fixture = CreateComplexFixture();
        using var repository = fixture.Repository.ToGitRepository();
        AssertReferences(fixture.Repository, repository);
        repository.References.Head.ShouldNotBeNull().TargetIdentifier.ShouldBe("refs/heads/main");
        repository.References.FromGlob("refs/heads/*").Select(r => r.Name.Canonical).Order()
            .ShouldBe(Lines(fixture.Repository.Run("for-each-ref", "--format=%(refname)", "refs/heads")).Order());
    }

    [Test]
    public void BranchesAndLookupsMatchGitCli()
    {
        using var fixture = CreateComplexFixture();
        using var repository = fixture.Repository.ToGitRepository();
        AssertBranches(fixture.Repository, repository);
        repository.Branches["missing"].ShouldBeNull();
        repository.Branches["develop"].ShouldNotBeNull().Tip.ShouldNotBeNull().Sha
            .ShouldBe(fixture.Repository.Run("rev-parse", "refs/heads/develop").Trim());
        repository.Branches["refs/heads/develop"].ShouldNotBeNull().Name.Canonical.ShouldBe("refs/heads/develop");
        repository.Head.Name.Canonical.ShouldBe("refs/heads/main");
        repository.IsHeadDetached.ShouldBeFalse();
        repository.IsShallow.ShouldBeFalse();
    }

    [Test]
    public void TagsMatchGitCli()
    {
        using var fixture = CreateComplexFixture();
        using var repository = fixture.Repository.ToGitRepository();
        AssertTags(fixture.Repository, repository);
    }

    [Test]
    public void CommitLogsMatchGitObjects()
    {
        using var fixture = CreateComplexFixture();
        using var repository = fixture.Repository.ToGitRepository();
        AssertCommits(fixture.Repository, repository);
    }

    [Test]
    public void QueryByPreservesReachabilityFirstParentAndSortContracts()
    {
        using var fixture = CreateCrissCrossFixture();
        using var repository = fixture.Repository.ToGitRepository();
        var commits = CollectAllCommits(repository);
        var sorts = new[] { CommitSortStrategies.None, CommitSortStrategies.Time, CommitSortStrategies.Topological, CommitSortStrategies.Topological | CommitSortStrategies.Time };
        foreach (var sort in sorts)
        {
            foreach (var firstParent in new[] { false, true })
            {
                foreach (var excluded in (string?[])[null, .. commits.Keys.Order().Take(3)])
                {
                    var filter = new CommitFilter
                    {
                        IncludeReachableFrom = repository.Branches["main"],
                        ExcludeReachableFrom = excluded == null ? null : commits[excluded],
                        FirstParentOnly = firstParent,
                        SortBy = sort
                    };
                    var actual = repository.Commits.QueryBy(filter).ToArray();
                    var args = new List<string> { "rev-list", "main" };
                    if (firstParent)
                    {
                        args.Add("--first-parent");
                    }

                    if (excluded != null)
                    {
                        args.Add("^" + excluded);
                    }

                    actual.Select(c => c.Sha).Order().ShouldBe(Lines(fixture.Repository.Run([.. args])).Order());
                    if (sort.HasFlag(CommitSortStrategies.Topological))
                    {
                        AssertTopological(actual);
                    }

                    repository.Commits.QueryBy(filter with { SortBy = sort | CommitSortStrategies.Reverse }).Select(c => c.Sha).ShouldBe(actual.Reverse().Select(c => c.Sha));
                }
            }
        }
    }

    [Test]
    public void MergeBasesMatchGitCliForAllCommitPairs()
    {
        using var fixture = CreateCrissCrossFixture();
        using var repository = fixture.Repository.ToGitRepository();
        AssertMergeBases(fixture.Repository, repository);
    }

    [Test]
    public void DiffPathsMatchGitCliForAllCommits()
    {
        using var fixture = CreateComplexFixture();
        using var repository = fixture.Repository.ToGitRepository();
        AssertDiffs(fixture.Repository, repository);
    }

    [Test]
    public void CountsModifiedStagedAndUntrackedPaths()
    {
        using var fixture = CreateDirtyFixture();
        using var repository = fixture.Repository.ToGitRepository();
        repository.UncommittedChangesCount().ShouldBe(3);
    }

    [Test]
    public void EmptyRepositoryHasUnbornMainAndUntrackedStatus()
    {
        using var fixture = new EmptyRepositoryFixture();
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "new.txt"), "new");
        using var repository = fixture.Repository.ToGitRepository();
        repository.Head.Name.Canonical.ShouldBe("refs/heads/main");
        repository.Head.Tip.ShouldBeNull();
        repository.IsHeadDetached.ShouldBeFalse();
        repository.UncommittedChangesCount().ShouldBe(1);
    }

    [Test]
    public void DetachedHeadRetainsTheSelectedCommit()
    {
        using var fixture = CreateComplexFixture();
        var target = fixture.Repository.Head.Tip.Parents.First().Sha;
        FixtureGit.Commands.Checkout(fixture.Repository, target);
        using var repository = fixture.Repository.ToGitRepository();
        repository.IsHeadDetached.ShouldBeTrue();
        repository.Head.IsDetachedHead.ShouldBeTrue();
        repository.Head.Tip.ShouldNotBeNull().Sha.ShouldBe(target);
    }

    [Test]
    public void RemotesAndTrackingBranchesMatchCloneConfiguration()
    {
        using var fixture = new RemoteRepositoryFixture();
        var local = fixture.LocalRepositoryFixture;
        using var repository = local.Repository.ToGitRepository();
        repository.Remotes.Select(r => r.Name).ShouldBe(["origin"]);
        var remote = repository.Remotes["origin"].ShouldNotBeNull();
        remote.Url.ShouldBe(local.Repository.Run("remote", "get-url", "origin").Trim());
        remote.FetchRefSpecs.Select(s => s.Specification).ShouldBe(["+refs/heads/*:refs/remotes/origin/*"]);
        remote.PushRefSpecs.ShouldBeEmpty();
        var fetch = remote.FetchRefSpecs.Single();
        fetch.Source.ShouldBe("refs/heads/*");
        fetch.Destination.ShouldBe("refs/remotes/origin/*");
        repository.Branches["main"].ShouldNotBeNull().IsTracking.ShouldBeTrue();
        repository.Branches["origin/main"].ShouldNotBeNull().IsRemote.ShouldBeTrue();
        AssertBranches(local.Repository, repository);
    }

    [Test]
    public void RepositoryPathsMatchGitCli()
    {
        using var fixture = CreateComplexFixture();
        using var repository = fixture.Repository.ToGitRepository();
        Path.GetFullPath(repository.Path).TrimEnd(Path.DirectorySeparatorChar).ShouldBe(Path.GetFullPath(fixture.Repository.Run("rev-parse", "--absolute-git-dir").Trim()).TrimEnd(Path.DirectorySeparatorChar));
        Path.GetFullPath(repository.WorkingDirectory).TrimEnd(Path.DirectorySeparatorChar).ShouldBe(Path.GetFullPath(fixture.Repository.Run("rev-parse", "--show-toplevel").Trim()).TrimEnd(Path.DirectorySeparatorChar));
    }

    [Test]
    public void BareRepositoryHasNoProjectRoot()
    {
        var path = FileSystemHelper.Path.GetRepositoryTempPath();
        FixtureGit.Repository.Init(path, isBare: true);
        try
        {
            var info = new ManagedGitRepositoryInfo(new FileSystem(), Options.Create(new GitVersionOptions { WorkingDirectory = path }));
            info.ProjectRootDirectory.ShouldBeNull();
            info.DotGitDirectory.ShouldNotBeNull().TrimEnd(Path.DirectorySeparatorChar).ShouldBe(Path.GetFullPath(path));
        }
        finally { FileSystemHelper.Directory.DeleteDirectory(path); }
    }

    [Test]
    public void OctopusMergeRetainsAllThreeParents()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("base");
        fixture.BranchTo("topic/one"); fixture.MakeACommit("one");
        fixture.Checkout("main"); fixture.BranchTo("topic/two"); fixture.MakeACommit("two");
        fixture.Checkout("main"); fixture.MakeACommit("main one");
        fixture.Repository.Run("merge", "topic/one", "topic/two", "-m", "octopus");
        using var repository = fixture.Repository.ToGitRepository();
        repository.Head.Tip.ShouldNotBeNull().Parents.Count.ShouldBe(3);
        AssertCommits(fixture.Repository, repository);
        AssertMergeBases(fixture.Repository, repository);
        AssertDiffs(fixture.Repository, repository);
    }

    [Test]
    public void LooseReferencesOverridePackedReferences()
    {
        using var fixture = CreateComplexFixture();
        fixture.Repository.Run("pack-refs", "--all");
        fixture.MakeACommit("shadow packed main"); fixture.BranchTo("loose/branch"); fixture.ApplyTag("loose-tag");
        using var repository = fixture.Repository.ToGitRepository();
        AssertReferences(fixture.Repository, repository);
        AssertBranches(fixture.Repository, repository);
        AssertTags(fixture.Repository, repository);
    }

    [Test]
    public void LightweightAndAnnotatedTagsPeelToTheSameCommit()
    {
        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("only commit"); fixture.ApplyTag("lightweight");
        fixture.Repository.Tags.Add("annotated", fixture.Repository.Head.Tip, Generate.SignatureNow(), "annotated");
        using var repository = fixture.Repository.ToGitRepository();
        AssertTags(fixture.Repository, repository);
        repository.Tags.Select(t => t.Commit.Sha).Distinct().Count().ShouldBe(1);
    }

    [Test]
    public void ShallowCloneStopsAtItsBoundary()
    {
        using var fixture = new RemoteRepositoryFixture();
        using var clone = fixture.CloneRepository(); clone.MakeShallow();
        using var repository = clone.Repository.ToGitRepository();
        repository.IsShallow.ShouldBeTrue();
        repository.Commits.Count().ShouldBe(1);
        AssertCommits(clone.Repository, repository);
    }

    [Test]
    public void LinkedWorktreeReadsItsOwnHeadAndSharedReferences()
    {
        using var fixture = CreateComplexFixture();
        var path = FileSystemHelper.Path.GetRepositoryTempPath();
        fixture.Repository.Run("worktree", "add", path, "develop");
        try
        {
            using var git = new FixtureGit.Repository(path);
            using var repository = git.ToGitRepository();
            repository.Head.Name.Canonical.ShouldBe("refs/heads/develop");
            repository.Head.Tip.ShouldNotBeNull().Sha.ShouldBe(git.Run("rev-parse", "HEAD").Trim());
            repository.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar).ShouldBe(path.TrimEnd(Path.DirectorySeparatorChar));
            repository.UncommittedChangesCount().ShouldBe(0);
            AssertReferences(git, repository); AssertCommits(git, repository);
        }
        finally { fixture.Repository.Run("worktree", "remove", "--force", path); }
    }

    [Test]
    public void IndexVersionFourRetainsDirtyPathCounts()
    {
        using var fixture = CreateDirtyFixture(); fixture.Repository.Run("update-index", "--index-version", "4");
        using var repository = fixture.Repository.ToGitRepository();
        repository.UncommittedChangesCount().ShouldBe(3);
    }

    private static string[] Lines(string output) => output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static void AssertReferences(FixtureGit.Repository git, IGitRepository repository) => repository.References.Select(r => $"{r.Name.Canonical} {r.TargetIdentifier}").Order()
        .ShouldBe(Lines(git.Run("for-each-ref", "--format=%(refname) %(objectname)")).Order());
    private static void AssertBranches(FixtureGit.Repository git, IGitRepository repository) => repository.Branches.Select(b => $"{b.Name.Canonical} {b.Tip?.Sha}").Order()
        .ShouldBe(Lines(git.Run("for-each-ref", "--format=%(refname) %(objectname)", "refs/heads", "refs/remotes")).Order());
    private static void AssertTags(FixtureGit.Repository git, IGitRepository repository)
    {
        repository.Tags.Select(t => t.Name.Canonical).Order().ShouldBe(Lines(git.Run("for-each-ref", "--format=%(refname)", "refs/tags")).Order());
        foreach (var tag in repository.Tags)
        {
            tag.TargetSha.ShouldBe(git.Run("rev-parse", tag.Name.Canonical + "^{}").Trim());
            tag.Commit.Sha.ShouldBe(git.Run("rev-parse", tag.Name.Canonical + "^{commit}").Trim());
        }
    }
    private static void AssertCommits(FixtureGit.Repository git, IGitRepository repository)
    {
        foreach (var branch in repository.Branches)
        {
            var actual = branch.Commits.ToArray();
            actual.Select(c => c.Sha).Order().ShouldBe(Lines(git.Run("rev-list", branch.Name.Canonical)).Order());
            foreach (var commit in actual)
            {
                commit.Message.TrimEnd().ShouldBe(git.Run("show", "-s", "--format=%B", commit.Sha).TrimEnd());
                commit.Parents.Select(p => p.Sha).ShouldBe(git.Run("show", "-s", "--format=%P", commit.Sha).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
                commit.IsMergeCommit.ShouldBe(commit.Parents.Count > 1);
            }
        }
        repository.Commits.Select(c => c.Sha).Order().ShouldBe(Lines(git.Run("rev-list", "HEAD")).Order());
    }
    private static void AssertTopological(IReadOnlyList<ICommit> commits)
    {
        var positions = commits.Select((c, i) => (c.Sha, i)).ToDictionary(p => p.Sha, p => p.i);
        foreach (var commit in commits)
        {
            foreach (var parent in commit.Parents.Where(p => positions.ContainsKey(p.Sha)))
            {
                positions[parent.Sha].ShouldBeGreaterThan(positions[commit.Sha]);
            }
        }
    }
    private static Dictionary<string, ICommit> CollectAllCommits(IGitRepository repository) => repository.Branches.SelectMany(b => b.Commits).DistinctBy(c => c.Sha).ToDictionary(c => c.Sha);
    private static void AssertMergeBases(FixtureGit.Repository git, IGitRepository repository)
    {
        var commits = CollectAllCommits(repository);
        foreach (var first in commits.Values)
        {
            foreach (var second in commits.Values)
            {
                var expected = Lines(git.Run("merge-base", "--all", first.Sha, second.Sha));
                expected.ShouldContain(repository.FindMergeBase(first, second).ShouldNotBeNull().Sha);
            }
        }
    }
    private static void AssertDiffs(FixtureGit.Repository git, IGitRepository repository)
    {
        foreach (var commit in CollectAllCommits(repository).Values)
        {
            var args = commit.Parents.FirstOrDefault() is { } parent
                ? new[] { "diff", "--no-renames", "--name-only", parent.Sha, commit.Sha }
                : new[] { "diff-tree", "--root", "--no-commit-id", "--no-renames", "--name-only", "-r", commit.Sha };
            commit.DiffPaths.ShouldBe(Lines(git.Run(args)));
        }
    }
    private static EmptyRepositoryFixture CreateDirtyFixture()
    {
        var fixture = new EmptyRepositoryFixture();
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "tracked.txt"), "one");
        FixtureGit.Commands.Stage(fixture.Repository, "tracked.txt");
        fixture.Repository.Commit("tracked", Generate.SignatureNow(), Generate.SignatureNow());
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "tracked.txt"), "two");
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "untracked.txt"), "new");
        File.WriteAllText(Path.Combine(fixture.RepositoryPath, "staged.txt"), "new"); FixtureGit.Commands.Stage(fixture.Repository, "staged.txt");
        return fixture;
    }
    private static EmptyRepositoryFixture CreateComplexFixture()
    {
        var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial"); fixture.ApplyTag("1.0.0");
        fixture.BranchTo("develop"); fixture.MakeACommit("develop one");
        fixture.Checkout("main"); fixture.MakeACommit("main two"); fixture.MergeNoFF("develop");
        fixture.Repository.Tags.Add("v2.0.0", fixture.Repository.Head.Tip, Generate.SignatureNow(), "annotated");
        fixture.BranchTo("feature/complex"); fixture.MakeACommit("feature one");
        fixture.Checkout("develop"); fixture.MakeACommit("develop two"); fixture.Checkout("main");
        return fixture;
    }
    private static EmptyRepositoryFixture CreateCrissCrossFixture()
    {
        var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("base"); fixture.BranchTo("develop"); fixture.MakeACommit("develop one"); var develop = fixture.Repository.Head.Tip;
        fixture.Checkout("main"); fixture.MakeACommit("main one"); var main = fixture.Repository.Head.Tip;
        var options = new FixtureGit.MergeOptions { FastForwardStrategy = FixtureGit.FastForwardStrategy.NoFastForward };
        fixture.Repository.Merge(develop, Generate.SignatureNow(), options);
        fixture.Checkout("develop"); fixture.Repository.Merge(main, Generate.SignatureNow(), options);
        fixture.MakeACommit("develop two"); fixture.Checkout("main"); fixture.MakeACommit("main two");
        return fixture;
    }
}
