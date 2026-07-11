using LibGit2Sharp;

namespace GitVersion.Git.Managed.Tests;

/// <summary>
/// Validates <see cref="GitRevisionWalker"/> ordering and merge-base results against libgit2
/// (via LibGit2Sharp) on the same fixture repositories — the parity GitVersion's version
/// calculation depends on.
/// </summary>
[TestFixture]
public class GitRevisionWalkerTests
{
    private static readonly string[] SortCombinations =
    [
        "None",
        "Time",
        "Topological",
        "Topological, Time",
        "Topological, Reverse",
        "Time, Reverse",
        "Topological, Time, Reverse"
    ];

    [TestCaseSource(nameof(SortCombinations))]
    public void LinearHistoryMatchesLibGit2(string sortName)
    {
        var sort = Enum.Parse<GitRevisionSort>(sortName);
        using var repository = CreateLinearRepository();

        AssertWalkParity(repository, new() { Sort = sort }, head: true);
    }

    [TestCaseSource(nameof(SortCombinations))]
    public void MergedHistoryMatchesLibGit2(string sortName)
    {
        var sort = Enum.Parse<GitRevisionSort>(sortName);
        using var repository = CreateMergedRepository();

        AssertWalkParity(repository, new() { Sort = sort }, head: true);
    }

    [TestCaseSource(nameof(SortCombinations))]
    public void MergedHistoryWithExcludesMatchesLibGit2(string sortName)
    {
        var sort = Enum.Parse<GitRevisionSort>(sortName);
        using var repository = CreateMergedRepository();
        var excluded = repository.RevParse("v0");

        var options = new GitRevisionWalkOptions { Sort = sort };
        options.Include.Add(repository.ResolveId("HEAD"));
        options.Exclude.Add(GitObjectId.Parse(excluded));

        AssertWalkParity(repository, options, head: false, excludeSha: excluded);
    }

    [TestCaseSource(nameof(SortCombinations))]
    public void EqualCommitterTimestampsMatchLibGit2(string sortName)
    {
        var sort = Enum.Parse<GitRevisionSort>(sortName);
        using var repository = CreateEqualTimestampRepository();

        AssertWalkParity(repository, new() { Sort = sort }, head: true);
    }

    [TestCaseSource(nameof(SortCombinations))]
    public void CrissCrossHistoryMatchesLibGit2(string sortName)
    {
        var sort = Enum.Parse<GitRevisionSort>(sortName);
        using var repository = CreateCrissCrossRepository();

        AssertWalkParity(repository, new() { Sort = sort }, head: true);
    }

    [TestCaseSource(nameof(SortCombinations))]
    public void OctopusMergeMatchesLibGit2(string sortName)
    {
        var sort = Enum.Parse<GitRevisionSort>(sortName);
        using var repository = CreateOctopusRepository();

        AssertWalkParity(repository, new() { Sort = sort }, head: true);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FirstParentWalksMatchLibGit2(bool withExclude)
    {
        using var repository = CreateMergedRepository();

        var options = new GitRevisionWalkOptions { FirstParentOnly = true };
        options.Include.Add(repository.ResolveId("HEAD"));
        string? excluded = null;

        if (withExclude)
        {
            excluded = repository.RevParse("v0");
            options.Exclude.Add(GitObjectId.Parse(excluded));
        }

        AssertWalkParity(repository, options, head: false, excludeSha: excluded);
    }

    [Test]
    public void MultipleIncludesMatchLibGit2()
    {
        using var repository = CreateMergedRepository();
        // Include two historic points rather than the merged tip.
        var first = repository.RevParse("HEAD^1^");
        var second = repository.RevParse("HEAD^2");

        var options = new GitRevisionWalkOptions { Sort = GitRevisionSort.Topological | GitRevisionSort.Time };
        options.Include.Add(GitObjectId.Parse(first));
        options.Include.Add(GitObjectId.Parse(second));

        using var store = repository.OpenObjectStore();
        var actual = new GitRevisionWalker(store).Walk(options).Select(commit => commit.Sha.ToString()).ToList();

        using var libgit2 = new Repository(repository.RepositoryPath);
        var filter = new global::LibGit2Sharp.CommitFilter
        {
            IncludeReachableFrom = new[] { first, second },
            SortBy = global::LibGit2Sharp.CommitSortStrategies.Topological | global::LibGit2Sharp.CommitSortStrategies.Time
        };
        var expected = libgit2.Commits.QueryBy(filter).Select(commit => commit.Sha).ToList();

        actual.ShouldBe(expected);
    }

    [Test]
    public void FindsTheMergeBaseOfDivergedBranches()
    {
        using var repository = CreateMergedRepository();

        AssertMergeBaseParity(repository, repository.RevParse("main"), repository.RevParse("feature"));
    }

    [Test]
    public void FindsTheMergeBaseOfCrissCrossBranches()
    {
        using var repository = CreateCrissCrossRepository();

        AssertMergeBaseParity(repository, repository.RevParse("main"), repository.RevParse("dev"));
        AssertMergeBaseParity(repository, repository.RevParse("dev"), repository.RevParse("main"));
    }

    [Test]
    public void TheMergeBaseOfACommitAndItsAncestorIsTheAncestor()
    {
        using var repository = CreateLinearRepository();
        var ancestor = repository.RevParse("HEAD~3");
        var tip = repository.RevParse("HEAD");

        using var store = repository.OpenObjectStore();
        var walker = new GitRevisionWalker(store);

        walker.FindMergeBase(GitObjectId.Parse(tip), GitObjectId.Parse(ancestor)).ShouldBe(GitObjectId.Parse(ancestor));
        walker.FindMergeBase(GitObjectId.Parse(ancestor), GitObjectId.Parse(tip)).ShouldBe(GitObjectId.Parse(ancestor));
        walker.FindMergeBase(GitObjectId.Parse(tip), GitObjectId.Parse(tip)).ShouldBe(GitObjectId.Parse(tip));
    }

    [Test]
    public void UnrelatedHistoriesHaveNoMergeBase()
    {
        using var repository = new GitTestRepository();
        repository.WriteFile("a.txt", "a\n");
        var main = repository.Commit("on main");
        repository.Run("checkout", "-q", "--orphan", "detached-root");
        repository.WriteFile("b.txt", "b\n");
        var orphan = repository.Commit("orphan root");

        using var store = repository.OpenObjectStore();
        new GitRevisionWalker(store)
            .FindMergeBase(GitObjectId.Parse(main), GitObjectId.Parse(orphan))
            .ShouldBeNull();
    }

    [Test]
    public void WalksUpToTheShallowBoundary()
    {
        using var repository = CreateLinearRepository();
        using var clone = new TempDirectory();
        repository.Run("clone", "-q", "--depth", "2", "file://" + repository.RepositoryPath, clone.FullPath);

        var layout = GitRepositoryLayout.Discover(clone.FullPath);
        using var store = layout.CreateObjectStore();

        var options = new GitRevisionWalkOptions();
        options.Include.Add(layout.CreateReferenceStore().ResolveToObjectId("HEAD")!.Value);

        // The walk stops at the shallow boundary instead of failing on the missing parent.
        new GitRevisionWalker(store).Walk(options).Count.ShouldBe(2);
    }

    private static void AssertWalkParity(GitTestRepository repository, GitRevisionWalkOptions options, bool head, string? excludeSha = null)
    {
        if (head)
        {
            options.Include.Add(repository.ResolveId("HEAD"));
        }

        using var store = repository.OpenObjectStore();
        var actual = new GitRevisionWalker(store).Walk(options).Select(commit => commit.Sha.ToString()).ToList();

        using var libgit2 = new Repository(repository.RepositoryPath);
        var filter = new global::LibGit2Sharp.CommitFilter
        {
            IncludeReachableFrom = options.Include[0].ToString(),
            SortBy = ToLibGit2Sort(options.Sort),
            FirstParentOnly = options.FirstParentOnly
        };

        if (excludeSha is not null)
        {
            filter.ExcludeReachableFrom = excludeSha;
        }

        var expected = libgit2.Commits.QueryBy(filter).Select(commit => commit.Sha).ToList();

        actual.ShouldBe(expected, $"sort: {options.Sort}, firstParent: {options.FirstParentOnly}, exclude: {excludeSha}");
        actual.ShouldNotBeEmpty();
    }

    private static void AssertMergeBaseParity(GitTestRepository repository, string first, string second)
    {
        using var store = repository.OpenObjectStore();
        var actual = new GitRevisionWalker(store).FindMergeBase(GitObjectId.Parse(first), GitObjectId.Parse(second));

        using var libgit2 = new Repository(repository.RepositoryPath);
        var expected = libgit2.ObjectDatabase.FindMergeBase(libgit2.Lookup<Commit>(first), libgit2.Lookup<Commit>(second));

        expected.ShouldNotBeNull();
        actual.ShouldNotBeNull();
        actual.Value.ToString().ShouldBe(expected.Sha);
    }

    private static global::LibGit2Sharp.CommitSortStrategies ToLibGit2Sort(GitRevisionSort sort)
    {
        var result = global::LibGit2Sharp.CommitSortStrategies.None;

        if (sort.HasFlag(GitRevisionSort.Topological))
        {
            result |= global::LibGit2Sharp.CommitSortStrategies.Topological;
        }

        if (sort.HasFlag(GitRevisionSort.Time))
        {
            result |= global::LibGit2Sharp.CommitSortStrategies.Time;
        }

        if (sort.HasFlag(GitRevisionSort.Reverse))
        {
            result |= global::LibGit2Sharp.CommitSortStrategies.Reverse;
        }

        return result;
    }

    private static GitTestRepository CreateLinearRepository()
    {
        var repository = new GitTestRepository();

        for (var i = 0; i < 6; i++)
        {
            repository.WriteFile("file.txt", $"content {i}\n");
            repository.Commit($"commit {i}");

            if (i == 0)
            {
                repository.Run("tag", "v0");
            }
        }

        return repository;
    }

    private static GitTestRepository CreateMergedRepository()
    {
        var repository = new GitTestRepository();
        repository.WriteFile("main.txt", "0\n");
        repository.Commit("main 0");
        repository.Run("tag", "v0");

        repository.Run("checkout", "-q", "-b", "feature");
        repository.WriteFile("feature.txt", "1\n");
        repository.Commit("feature 1");
        repository.WriteFile("feature.txt", "2\n");
        repository.Commit("feature 2");

        repository.Run("checkout", "-q", "main");
        repository.WriteFile("main.txt", "1\n");
        repository.Commit("main 1");
        repository.WriteFile("main.txt", "2\n");
        repository.Commit("main 2");

        repository.Merge("feature");
        return repository;
    }

    private static GitTestRepository CreateEqualTimestampRepository()
    {
        var repository = new GitTestRepository();
        repository.WriteFile("main.txt", "0\n");
        repository.Commit("main 0");

        repository.Run("checkout", "-q", "-b", "feature");
        repository.WriteFile("feature.txt", "1\n");
        repository.Commit("feature 1", advanceClock: false);
        repository.WriteFile("feature.txt", "2\n");
        repository.Commit("feature 2", advanceClock: false);

        repository.Run("checkout", "-q", "main");
        repository.WriteFile("main.txt", "1\n");
        repository.Commit("main 1", advanceClock: false);
        repository.WriteFile("main.txt", "2\n");
        repository.Commit("main 2", advanceClock: false);

        repository.Merge("feature", advanceClock: false);
        return repository;
    }

    private static GitTestRepository CreateCrissCrossRepository()
    {
        var repository = new GitTestRepository();
        repository.WriteFile("main.txt", "0\n");
        repository.Commit("root");

        repository.Run("checkout", "-q", "-b", "dev");
        repository.WriteFile("dev.txt", "1\n");
        var devCommit = repository.Commit("dev 1");

        repository.Run("checkout", "-q", "main");
        repository.WriteFile("main.txt", "1\n");
        var mainCommit = repository.Commit("main 1");

        // Merge in both directions to create a criss-cross: two best common ancestors.
        repository.Merge(devCommit);
        repository.Run("checkout", "-q", "dev");
        repository.Merge(mainCommit);

        return repository;
    }

    private static GitTestRepository CreateOctopusRepository()
    {
        var repository = new GitTestRepository();
        repository.WriteFile("main.txt", "0\n");
        repository.Commit("root");

        foreach (var branch in new[] { "b1", "b2" })
        {
            repository.Run("checkout", "-q", "-b", branch, "main");
            repository.WriteFile($"{branch}.txt", "1\n");
            repository.Commit($"{branch} 1");
        }

        repository.Run("checkout", "-q", "main");
        repository.WriteFile("main.txt", "1\n");
        repository.Commit("main 1");
        repository.Merge("b1", "b2");

        return repository;
    }
}
