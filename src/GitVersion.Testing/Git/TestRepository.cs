using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Testing.Extensions;
using GitVersion.Testing.Internal;
using Shouldly;

namespace GitVersion.Testing;

/// <summary>
///     A test repository whose write operations are performed by shelling out to the real `git` executable
///     with a deterministic environment (fixed identities, virtual commit timestamps, isolated configuration).
/// </summary>
public sealed class TestRepository : IDisposable
{
    private static int _pad = 1;

    public TestRepository(string path)
    {
        Path = FileSystemHelper.Path.GetFullPath(path).TrimEnd('/', '\\');
        Branches = new(this);
        Tags = new(this);
        Network = new(this);
        Refs = new(this);
        Worktrees = new(this);
        Config = new(this);
    }

    /// <summary>
    ///     The working directory of the repository (no trailing directory separator).
    /// </summary>
    public string Path { get; }

    public TestBranchCollection Branches { get; }

    public TestTagCollection Tags { get; }

    public TestNetwork Network { get; }

    public TestReferenceCollection Refs { get; }

    public TestWorktreeCollection Worktrees { get; }

    public TestConfig Config { get; }

    /// <summary>
    ///     The branch (or detached state) HEAD currently points at.
    /// </summary>
    public TestBranch Head
    {
        get
        {
            // Fast path: read .git/HEAD directly (no process spawn) for the common attached-HEAD case.
            if (TryReadGitFile("HEAD", out var head) && head.StartsWith("ref: ", StringComparison.Ordinal))
            {
                var canonicalName = head["ref: ".Length..].Trim();
                var friendlyName = canonicalName.StartsWith("refs/heads/", StringComparison.Ordinal)
                    ? canonicalName["refs/heads/".Length..]
                    : canonicalName;
                return new(this, canonicalName, friendlyName, "HEAD");
            }

            if (TryRun(["symbolic-ref", "--quiet", "HEAD"], out var output))
            {
                var canonicalName = output.Trim();
                return new(this, canonicalName, canonicalName["refs/heads/".Length..], "HEAD");
            }

            return new(this, "(no branch)", "(no branch)", "HEAD");
        }
    }

    /// <summary>
    ///     Creates a commit of a new random file, mirroring the behavior of the previous library-based helper.
    /// </summary>
    public TestCommit MakeACommit(string? commitMessage = null) => CreateFileAndCommit(Guid.NewGuid().ToString(), commitMessage);

    public TestCommit[] MakeCommits(int numCommitsToMake)
        => [.. Enumerable.Range(1, numCommitsToMake).Select(_ => MakeACommit())];

    public TestTag MakeATaggedCommit(string tag)
    {
        var commit = MakeACommit();
        var existingTag = Tags.SingleOrDefault(t => t.FriendlyName == tag);
        return existingTag ?? Tags.Add(tag, commit);
    }

    /// <summary>
    ///     Creates a lightweight tag pointing at HEAD.
    /// </summary>
    public TestTag ApplyTag(string tag) => Tags.Add(tag, "HEAD");

    /// <summary>
    ///     Creates a branch pointing at HEAD without checking it out.
    /// </summary>
    public TestBranch CreateBranch(string branchName) => Branches.Add(branchName, "HEAD");

    /// <summary>
    ///     Creates a branch pointing at the given commit without checking it out.
    /// </summary>
    public TestBranch CreateBranch(string branchName, TestCommit target) => Branches.Add(branchName, target);

    public void Checkout(string committish) => Run("checkout", committish);

    public void Checkout(TestBranch branch) => Checkout(branch.FriendlyName);

    public void Checkout(TestCommit commit) => Run("checkout", "--detach", commit.Sha);

    /// <summary>
    ///     Stages the given file (relative or absolute path).
    /// </summary>
    public void Stage(string path) => Run("add", "--", path);

    public TestCommit Commit(string message, Signature author, Signature committer, CommitOptions? options = null)
    {
        List<string> arguments = ["commit", "--message", message];
        if (options?.AmendPreviousCommit == true)
        {
            arguments.Add("--amend");
        }

        if (options?.AllowEmptyCommit == true)
        {
            arguments.Add("--allow-empty");
        }

        Run(author, committer, [.. arguments]);
        return new(this, ResolveHeadSha());
    }

    public void Merge(string committish, Signature merger, MergeOptions? options = null)
    {
        List<string> arguments = ["merge", "--no-edit"];
        switch (options?.FastForwardStrategy ?? FastForwardStrategy.Default)
        {
            case FastForwardStrategy.NoFastForward:
                arguments.Add("--no-ff");
                break;
            case FastForwardStrategy.FastForwardOnly:
                arguments.Add("--ff-only");
                break;
            case FastForwardStrategy.Default:
                break;
        }

        arguments.Add(committish);
        Run(merger, merger, [.. arguments]);
    }

    public void Merge(TestBranch branch, Signature merger, MergeOptions? options = null) => Merge(branch.FriendlyName, merger, options);

    public void Merge(TestCommit commit, Signature merger, MergeOptions? options = null) => Merge(commit.Sha, merger, options);

    public void MergeNoFF(string branch) => MergeNoFF(branch, Generate.SignatureNow());

    public void MergeNoFF(string branch, Signature sig) => Merge(branch, sig, new() { FastForwardStrategy = FastForwardStrategy.NoFastForward });

    public TestCommit CreatePullRequestRef(string from, string to, int prNumber = 2, bool normalise = false, bool allowFastForwardMerge = false)
    {
        var toTip = Branches[to].ShouldNotBeNull().Tip;
        Checkout(toTip);
        if (allowFastForwardMerge)
        {
            Merge(from, Generate.SignatureNow());
        }
        else
        {
            MergeNoFF(from);
        }

        var commit = Head.Tip;
        Refs.Add($"refs/pull/{prNumber}/merge", commit.Sha);
        Checkout(to);
        if (normalise)
        {
            // Turn the ref into a real branch
            Checkout(Branches.Add($"pull/{prNumber}/merge", commit));
        }

        return commit;
    }

    public void Fetch(string remote, IEnumerable<string>? refspecs = null)
    {
        var specs = refspecs?.ToArray() ?? [];
        if (specs.Length == 0)
        {
            // Mirror the previous git-library behavior where an empty refspec downloaded the objects
            // for every advertised ref (including custom pull/merge-request refs). Standard heads land
            // in remote-tracking space; every other ref is mirrored into an isolated namespace purely to
            // pull its objects into the object database without creating refs that branch discovery reads.
            specs =
            [
                $"+refs/heads/*:refs/remotes/{remote}/*",
                $"+refs/*:refs/{remote}-fetched/*"
            ];
        }

        Run(["fetch", remote, .. specs]);
    }

    /// <summary>
    ///     The paths changed between two commits, using git-native forward slashes.
    /// </summary>
    public IReadOnlyList<string> DiffPaths(TestCommit oldCommit, TestCommit newCommit)
        => [.. Run("diff", "--name-only", oldCommit.Sha, newCommit.Sha).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    public void DumpGraph(Action<string>? writer = null, int? maxCommits = null)
        => GitTestExtensions.ExecuteGitCmd(GitExtensions.CreateGitLogArgs(maxCommits), Path, writer);

    /// <summary>
    ///     Resolves a committish to the full sha it points at.
    /// </summary>
    public string RevParse(string committish) => Run("rev-parse", "--verify", $"{committish}^{{commit}}").Trim();

    /// <summary>
    ///     Looks up the commit the given committish points at, or <c>null</c> when the object is not present
    ///     (mirroring the previous git-library <c>Lookup</c> semantics).
    /// </summary>
    public TestCommit? Lookup(string committish)
    {
        var log = GetLog(committish, maxCount: 1);
        return log.Count > 0 ? log[0] : null;
    }

    /// <summary>
    ///     Looks up a commit that is expected to exist, throwing when it does not.
    /// </summary>
    internal TestCommit LookupRequired(string committish)
        => Lookup(committish) ?? throw new InvalidOperationException($"No commit found for '{committish}'.");

    internal IReadOnlyList<TestCommit> GetLog(string committish, int? maxCount = null)
    {
        List<string> arguments = ["log", "--format=%x1e%H%x1f%an%x1f%ae%x1f%aI%x1f%cn%x1f%ce%x1f%cI%x1f%P%x1f%B"];
        if (maxCount is not null)
        {
            arguments.Add($"--max-count={maxCount}");
        }

        arguments.Add(committish);
        arguments.Add("--");
        if (!TryRun([.. arguments], out var output))
        {
            // Unknown committish (e.g. an object that has not been fetched yet): treat as no commits.
            return [];
        }

        return
        [
            .. output
                .Split('\x1e', StringSplitOptions.RemoveEmptyEntries)
                .Where(record => record.Contains('\x1f'))
                .Select(ParseCommitRecord)
        ];
    }

    private TestCommit ParseCommitRecord(string record)
    {
        var fields = record.Split('\x1f');
        var author = new Signature(fields[1], fields[2], DateTimeOffset.Parse(fields[3]));
        var committer = new Signature(fields[4], fields[5], DateTimeOffset.Parse(fields[6]));
        var parents = fields[7].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var message = fields[8].TrimEnd('\r', '\n');
        return new(this, fields[0], message, author, committer, parents);
    }

    internal string Run(params string[] arguments) => GitCliRunner.Run(Path, arguments);

    internal string Run(Signature author, Signature committer, string[] arguments) => GitCliRunner.Run(Path, arguments, author, committer);

    internal bool TryRun(string[] arguments) => TryRun(arguments, out _);

    internal bool TryRun(string[] arguments, out string output) => GitCliRunner.TryRun(Path, arguments, out output, out _) == 0;

    /// <summary>
    ///     Resolves the sha HEAD points at, reading the ref files directly when possible (no process spawn)
    ///     and falling back to <c>git rev-parse</c> for packed refs or any unexpected layout.
    /// </summary>
    private string ResolveHeadSha()
    {
        if (TryReadGitFile("HEAD", out var head))
        {
            if (head.StartsWith("ref: ", StringComparison.Ordinal))
            {
                var refPath = head["ref: ".Length..].Trim();
                if (TryReadGitFile(refPath, out var refSha) && IsSha(refSha))
                {
                    return refSha;
                }
            }
            else if (IsSha(head))
            {
                return head;
            }
        }

        return RevParse("HEAD");
    }

    private static bool IsSha(string value) => value.Length == 40 && value.All(Uri.IsHexDigit);

    private string? gitDirectory;

    private string GitDirectory => this.gitDirectory ??= ResolveGitDirectory();

    private string ResolveGitDirectory()
    {
        var dotGit = FileSystemHelper.Path.Combine(Path, ".git");
        if (FileSystemHelper.Directory.Exists(dotGit))
        {
            return dotGit;
        }

        // Worktrees and submodules use a `.git` file that points at the real git directory.
        if (FileSystemHelper.File.Exists(dotGit))
        {
            var content = FileSystemHelper.File.ReadAllText(dotGit).Trim();
            const string prefix = "gitdir:";
            if (content.StartsWith(prefix, StringComparison.Ordinal))
            {
                var dir = content[prefix.Length..].Trim();
                return FileSystemHelper.Path.IsPathRooted(dir)
                    ? dir
                    : FileSystemHelper.Path.GetFullPath(FileSystemHelper.Path.Combine(Path, dir));
            }
        }

        return dotGit;
    }

    private bool TryReadGitFile(string relativePath, out string content)
    {
        try
        {
            var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var fullPath = segments.Aggregate(GitDirectory, (current, segment) => FileSystemHelper.Path.Combine(current, segment));
            if (FileSystemHelper.File.Exists(fullPath))
            {
                content = FileSystemHelper.File.ReadAllText(fullPath).Trim();
                return true;
            }
        }
        catch (IOException)
        {
            // Fall through to the caller's process-based fallback.
        }

        content = string.Empty;
        return false;
    }

    private TestCommit CreateFileAndCommit(string relativeFileName, string? commitMessage = null)
    {
        var randomFile = FileSystemHelper.Path.Combine(Path, relativeFileName);
        if (FileSystemHelper.File.Exists(randomFile))
        {
            FileSystemHelper.File.Delete(randomFile);
        }

        var totalWidth = 36 + (_pad++ % 10);
        var contents = Guid.NewGuid().ToString().PadRight(totalWidth, '.');
        FileSystemHelper.File.WriteAllText(randomFile, contents);

        Stage(randomFile);

        return Commit(commitMessage ?? $"Test Commit for file '{relativeFileName}'",
            Generate.SignatureNow(), Generate.SignatureNow());
    }

    /// <summary>
    ///     Clones the repository at <paramref name="sourcePath" /> into <paramref name="targetPath" />.
    /// </summary>
    public static TestRepository Clone(string sourcePath, string targetPath)
    {
        GitCliRunner.Run(FileSystemHelper.Path.GetTempPath(), ["clone", sourcePath, targetPath]);
        return new(targetPath);
    }

    public void Dispose()
    {
        // Nothing to release; present so fixtures can treat the repository as a disposable resource.
    }
}
