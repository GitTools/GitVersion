using System.Globalization;

namespace GitVersion.Testing.Git;

/// <summary>
/// Repository arrangement for integration tests. Every operation uses Git itself,
/// independently of the managed reader under test. This deliberately exposes only
/// the small fixture surface needed by the existing scenario builders.
/// </summary>
public sealed class Repository : IRepository
{
    private readonly List<WeakReference<GitVersion.Git.IGitRepository>> readers = [];
    private readonly Dictionary<string, Commit> commits = new(StringComparer.Ordinal);
    public Repository(string path)
    {
        DirectoryPath = Path.GetFullPath(path);
        Info = new(this);
        Config = new(this);
        Branches = new(this);
        Tags = new(this);
        Refs = new(this);
        Network = new(this);
        ObjectDatabase = new(this);
        Worktrees = new(this);
        Diff = new(this);
    }

    internal string DirectoryPath { get; }
    public RepositoryInformation Info { get; }
    public Configuration Config { get; }
    public BranchCollection Branches { get; }
    public TagCollection Tags { get; }
    public ReferenceCollection Refs { get; }
    public Network Network { get; }
    public ObjectDatabase ObjectDatabase { get; }
    public WorktreeCollection Worktrees { get; }
    public Diff Diff { get; }
    public Branch Head => new(this, Run(false, "symbolic-ref", "-q", "HEAD").Trim() is { Length: > 0 } name ? name : "(no branch)");
    public bool IsHeadDetached => Head.CanonicalName == "(no branch)";
    public IEnumerable<Commit> Commits => Log("HEAD");

    public string Run(params string[] arguments) => Run(true, arguments);

    internal void Observe(GitVersion.Git.IGitRepository reader) => this.readers.Add(new(reader));

    private void RefreshReaders()
    {
        foreach (var weak in this.readers)
        {
            if (weak.TryGetTarget(out var reader))
            {
                reader.DiscoverRepository(Info.WorkingDirectory);
            }
        }
    }

    internal string Run(bool checkExitCode, params string[] arguments)
    {
        var output = Execute(DirectoryPath, checkExitCode, null, null, arguments);
        var readOnly = arguments[0] is "rev-parse" or "cat-file" or "show" or "rev-list" or "for-each-ref" or "show-ref" or "ls-remote" or "diff"
            || arguments[0] == "symbolic-ref" && arguments.Length > 1 && arguments[1] == "-q";
        if (!readOnly)
        {
            RefreshReaders();
        }
        return output;
    }

    public string RunWithSignature(Signature author, Signature committer, string? input, params string[] arguments)
    {
        var output = Execute(DirectoryPath, true, (author, committer), input, arguments);
        RefreshReaders();
        return output;
    }

    internal static string Execute(string directory, bool checkExitCode, (Signature Author, Signature Committer)? signatures, string? input, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = input != null,
            UseShellExecute = false
        };
        foreach (var name in start.Environment.Keys.Where(name => name.StartsWith("GIT_", StringComparison.Ordinal)).ToArray())
        {
            start.Environment.Remove(name);
        }
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["LC_ALL"] = "C";
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("commit.gpgsign=false");
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("tag.gpgsign=false");
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("gc.auto=0");
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("protocol.file.allow=always");
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        if (signatures is { } identity)
        {
            SetSignature("AUTHOR", identity.Author);
            SetSignature("COMMITTER", identity.Committer);
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start Git for repository fixture.");
        // Parallel NUnit workers block while arranging repositories. On .NET 10,
        // pipe ReadToEndAsync completion also needs thread-pool workers, causing
        // starvation across thousands of short Git invocations. Drain both pipes
        // on dedicated readers so large stderr/stdout cannot deadlock either.
        var stdout = ReadOnThread(process.StandardOutput);
        var stderr = ReadOnThread(process.StandardError);
        if (input != null)
        {
            process.StandardInput.Write(input);
            process.StandardInput.Close();
        }
        process.WaitForExit();
        var output = stdout.GetAwaiter().GetResult();
        var error = stderr.GetAwaiter().GetResult();
        if (checkExitCode && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed ({process.ExitCode}) in '{directory}': {error}");
        }
        return process.ExitCode == 0 ? output : string.Empty;

        void SetSignature(string kind, Signature signature)
        {
            start.Environment[$"GIT_{kind}_NAME"] = signature.Name;
            start.Environment[$"GIT_{kind}_EMAIL"] = signature.Email;
            start.Environment[$"GIT_{kind}_DATE"] = signature.When.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        }

        static Task<string> ReadOnThread(StreamReader reader)
        {
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            new Thread(() =>
            {
                try { completion.SetResult(reader.ReadToEnd()); }
                catch (Exception exception) { completion.SetException(exception); }
            })
            { IsBackground = true }.Start();
            return completion.Task;
        }
    }

    public static string Init(string path, bool isBare = false, string initialBranch = "main")
    {
        Execute(SysEnv.CurrentDirectory, true, null, null, "init", "-b", initialBranch, isBare ? "--bare" : "--quiet", path);
        return Path.GetFullPath(isBare ? path : Path.Combine(path, ".git")) + Path.DirectorySeparatorChar;
    }

    public static string Clone(string source, string destination)
    {
        Execute(SysEnv.CurrentDirectory, true, null, null, "clone", source, destination);
        return Path.Combine(destination, ".git");
    }

    public Branch CreateBranch(string name) => CreateBranch(name, "HEAD");
    public Branch CreateBranch(string name, Commit commit) => CreateBranch(name, commit.Sha);
    public Branch CreateBranch(string name, string target)
    {
        Run("branch", name, target);
        return new(this, name.StartsWith("refs/", StringComparison.Ordinal) ? name : "refs/heads/" + name);
    }

    public Commit Commit(string message, Signature author, Signature committer, CommitOptions? options = null)
    {
        var arguments = new List<string> { "commit", "--quiet", "--allow-empty", "--allow-empty-message", "-m", message };
        if (options?.AmendPreviousCommit == true)
        {
            arguments.Add("--amend");
        }

        RunWithSignature(author, committer, null, [.. arguments]);
        return Lookup<Commit>("HEAD")!;
    }

    public void Merge(string target, Signature signature, MergeOptions? options = null) => RunWithSignature(signature, signature, null, "merge", "--no-edit", options?.FastForwardStrategy == FastForwardStrategy.NoFastForward ? "--no-ff" : "--ff", target);
    public void Merge(Branch target, Signature signature, MergeOptions? options = null) => Merge(target.CanonicalName, signature, options);
    public void Merge(Commit target, Signature signature, MergeOptions? options = null) => Merge(target.Sha, signature, options);

    public Tag ApplyTag(string name) => Tags.Add(name, "HEAD");
    public Tag ApplyTag(string name, string target) => Tags.Add(name, target);
    public Tag ApplyTag(string name, Signature signature, string message) => Tags.Add(name, Head.Tip, signature, message);
    public Tag ApplyTag(string name, string target, Signature signature, string message) => Tags.Add(name, target, signature, message);

    public GitObject? Lookup(string revision)
    {
        var sha = Run(false, "rev-parse", "--verify", revision).Trim();
        if (sha.Length == 0)
        {
            return null;
        }

        return Run(false, "cat-file", "-t", sha).Trim() switch
        {
            "commit" => GetCommit(sha),
            "tree" => new Tree(this, sha),
            "" => null,
            _ => new GitObject(this, sha)
        };
    }
    public T? Lookup<T>(string revision) where T : GitObject
    {
        if (typeof(T) != typeof(Commit))
        {
            return Lookup(revision) as T;
        }

        var sha = Run(false, "rev-parse", "--verify", revision + "^{commit}").Trim();
        return sha.Length == 0 ? null : (T)(GitObject)GetCommit(sha);
    }
    internal Commit GetCommit(string sha)
    {
        if (this.commits.TryGetValue(sha, out var commit))
        {
            return commit;
        }

        return this.commits[sha] = new(this, sha);
    }
    internal IEnumerable<Commit> Log(string revision) => Run(false, "rev-list", "--date-order", revision).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(sha => GetCommit(sha.Trim()));
    public void Dispose()
    {
        foreach (var weak in this.readers)
        {
            if (weak.TryGetTarget(out var reader))
            {
                reader.Dispose();
            }
        }
        this.readers.Clear();
        this.commits.Clear();
    }
}

public interface IRepository : IDisposable
{
    RepositoryInformation Info { get; }
    Configuration Config { get; }
    BranchCollection Branches { get; }
    TagCollection Tags { get; }
    ReferenceCollection Refs { get; }
    Network Network { get; }
    ObjectDatabase ObjectDatabase { get; }
    WorktreeCollection Worktrees { get; }
    Diff Diff { get; }
    Branch Head { get; }
    IEnumerable<Commit> Commits { get; }
    Commit Commit(string message, Signature author, Signature committer, CommitOptions? options = null);
    void Merge(string target, Signature signature, MergeOptions? options = null);
    void Merge(Branch target, Signature signature, MergeOptions? options = null);
    void Merge(Commit target, Signature signature, MergeOptions? options = null);
    string Run(params string[] arguments);
    string RunWithSignature(Signature author, Signature committer, string? input, params string[] arguments);
}

public sealed record Signature(string Name, string Email, DateTimeOffset When);
public sealed record ObjectId(string Sha)
{
    public override string ToString() => Sha;
}
public class GitObject(Repository repository, string sha)
{
    internal Repository Repository { get; } = repository;
    public string Sha { get; } = sha;
    public ObjectId Id => new(Sha);
}
public sealed class Commit(Repository repository, string sha) : GitObject(repository, sha)
{
    private string[] Metadata => field ??= Repository.Run("show", "-s", "--format=%P%x00%T%x00%an%x00%ae%x00%aI%x00%cn%x00%ce%x00%cI%x00%B", Sha).Split('\0');
    public IEnumerable<Commit> Parents => Metadata[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Repository.GetCommit);
    public Tree Tree => new(Repository, Metadata[1]);
    public Signature Author => new(Metadata[2], Metadata[3], DateTimeOffset.Parse(Metadata[4], CultureInfo.InvariantCulture));
    public Signature Committer => new(Metadata[5], Metadata[6], DateTimeOffset.Parse(Metadata[7], CultureInfo.InvariantCulture));
    public string Message => Metadata[8];
    public string MessageShort => Message.Split('\n')[0];
}
public sealed class Tree(Repository repository, string sha) : GitObject(repository, sha);
public sealed class CommitOptions
{
    public bool AmendPreviousCommit { get; set; }
    public bool AllowEmptyCommit { get; set; }
}
public enum FastForwardStrategy { Default, NoFastForward }
public sealed class MergeOptions
{
    public FastForwardStrategy FastForwardStrategy { get; set; }
}
public sealed class FetchOptions;
public sealed class PullOptions;

public sealed class RepositoryInformation(Repository repository)
{
    public string WorkingDirectory => System.IO.Path.GetFullPath(repository.DirectoryPath).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
    public string Path => repository.Run("rev-parse", "--absolute-git-dir").Trim() + System.IO.Path.DirectorySeparatorChar;
    public bool IsHeadDetached => repository.IsHeadDetached;
}
public sealed class Configuration(Repository repository)
{
    public void Set<T>(string key, T value) => repository.Run("config", key, Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
    public Signature BuildSignature(DateTimeOffset when) => new(repository.Run("config", "user.name").Trim(), repository.Run("config", "user.email").Trim(), when);
}

public sealed class Branch(Repository repository, string name)
{
    public string CanonicalName { get; } = name;
    public string FriendlyName => CanonicalName.Replace("refs/heads/", "", StringComparison.Ordinal).Replace("refs/remotes/", "", StringComparison.Ordinal);
    public bool IsRemote => CanonicalName.StartsWith("refs/remotes/", StringComparison.Ordinal);
    public Commit Tip => repository.Lookup<Commit>(CanonicalName == "(no branch)" ? "HEAD" : CanonicalName)!;
    public IEnumerable<Commit> Commits => repository.Log(CanonicalName == "(no branch)" ? "HEAD" : CanonicalName);
    public string RemoteName => repository.Run(false, "config", $"branch.{FriendlyName}.remote").Trim();
    public Branch? TrackedBranch => repository.Branches[repository.Run(false, "rev-parse", "--symbolic-full-name", FriendlyName + "@{upstream}").Trim()];
}
public sealed class BranchUpdater
{
    public string? Remote { get; set; }
    public string? UpstreamBranch { get; set; }
    public string? TrackedBranch { get; set; }
}
public sealed class BranchCollection(Repository repository) : IEnumerable<Branch>
{
    public Branch? this[string name]
    {
        get
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var canonical = name.StartsWith("refs/", StringComparison.Ordinal) ? name : "refs/heads/" + name;
            if (repository.Run(false, "show-ref", "--verify", canonical).Length == 0)
            {
                canonical = "refs/remotes/" + name;
                if (repository.Run(false, "show-ref", "--verify", canonical).Length == 0)
                {
                    return null;
                }
            }
            return new(repository, canonical);
        }
    }
    public Branch Add(string name, Commit commit) => repository.CreateBranch(name, commit);
    public void Remove(string name)
    {
        if (this[name] is { } branch)
        {
            Remove(branch);
        }
    }
    public void Remove(Branch branch)
    {
        if (!branch.IsRemote && branch.CanonicalName == repository.Head.CanonicalName)
        {
            throw new InvalidOperationException("Cannot remove the currently checked-out branch.");
        }
        repository.Run("update-ref", "-d", branch.CanonicalName);
    }
    public Branch Update(Branch branch, Action<BranchUpdater> update)
    {
        var values = new BranchUpdater();
        update(values);
        if (values.Remote != null)
        {
            repository.Config.Set($"branch.{branch.FriendlyName}.remote", values.Remote);
        }

        if (values.UpstreamBranch != null)
        {
            repository.Config.Set($"branch.{branch.FriendlyName}.merge", values.UpstreamBranch);
        }

        if (values.TrackedBranch != null)
        {
            repository.Run("branch", "--set-upstream-to", values.TrackedBranch, branch.FriendlyName);
        }

        return branch;
    }
    public IEnumerator<Branch> GetEnumerator() => repository.Run("for-each-ref", "--format=%(refname)", "refs/heads", "refs/remotes").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(name => new Branch(repository, name.Trim())).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public sealed class Tag(Repository repository, string name)
{
    public string FriendlyName { get; } = name;
    public string CanonicalName => "refs/tags/" + FriendlyName;
    public GitObject Target => repository.Lookup(CanonicalName)!;
    public GitObject PeeledTarget => repository.Lookup(CanonicalName + "^{}")!;
}
public sealed class TagCollection(Repository repository) : IEnumerable<Tag>
{
    public Tag this[string name] => new(repository, name);
    public Tag Add(string name, Commit commit) => Add(name, commit.Sha);
    public Tag Add(string name, string target)
    {
        repository.Run("tag", name, target);
        return this[name];
    }
    public Tag Add(string name, Commit commit, Signature signature, string message) => Add(name, commit.Sha, signature, message);
    public Tag Add(string name, string target, Signature signature, string message)
    {
        repository.RunWithSignature(signature, signature, null, "tag", "-a", name, target, "-m", message);
        return this[name];
    }
    public void Remove(string name) => repository.Run("tag", "-d", name);
    public IEnumerator<Tag> GetEnumerator() => repository.Run("tag", "--list").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(name => this[name.Trim()]).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public sealed record Reference(string CanonicalName, string TargetIdentifier = "");
public sealed class ReferenceCollection(Repository repository) : IEnumerable<Reference>
{
    public Reference this[string name] => new(name);
    public IEnumerator<Reference> GetEnumerator() => repository.Run("for-each-ref", "--format=%(refname) %(objectname)").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Split(' ')).Select(parts => new Reference(parts[0], parts[1])).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public void Add(string name, ObjectId id, bool allowOverwrite = false) => Add(name, id.Sha, allowOverwrite);
    public void Add(string name, string target, bool allowOverwrite = false)
    {
        if (target.StartsWith("refs/", StringComparison.Ordinal))
        {
            repository.Run("symbolic-ref", name, target);
        }
        else if (allowOverwrite)
        {
            repository.Run("update-ref", name, target);
        }
        else
        {
            repository.Run("update-ref", name, target, new string('0', 40));
        }
    }
    public void Remove(string name) => repository.Run("update-ref", "-d", name);
    public void UpdateTarget(Reference reference, string target)
    {
        if (target.StartsWith("refs/", StringComparison.Ordinal))
        {
            repository.Run("symbolic-ref", reference.CanonicalName, target);
        }
        else
        {
            repository.Run("update-ref", reference.CanonicalName, target);
        }
    }
    public void UpdateTarget(Reference reference, ObjectId id) => repository.Run("update-ref", reference.CanonicalName, id.Sha);
}
public sealed class Remote(Repository repository, string name)
{
    public string Name { get; } = name;
    public string Url => repository.Run("remote", "get-url", Name).Trim();
}
public sealed class RemoteUpdater
{
    public string? Url { get; set; }
    public string? FetchRefSpecs { get; set; }
}
public sealed class RemoteCollection(Repository repository) : IEnumerable<Remote>
{
    public Remote this[string name] => new(repository, name);
    public Remote Add(string name, string url)
    {
        repository.Run("remote", "add", name, url);
        return this[name];
    }
    public void Remove(string name) => repository.Run("remote", "remove", name);
    public void Update(string name, Action<RemoteUpdater> update)
    {
        var values = new RemoteUpdater();
        update(values);
        if (values.Url != null)
        {
            repository.Run("remote", "set-url", name, values.Url);
        }

        if (values.FetchRefSpecs != null)
        {
            repository.Run("config", "--replace-all", $"remote.{name}.fetch", values.FetchRefSpecs);
        }
    }
    public IEnumerator<Remote> GetEnumerator() => repository.Run("remote").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(name => this[name.Trim()]).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public sealed class Network(Repository repository)
{
    public RemoteCollection Remotes { get; } = new(repository);
}
public sealed class ObjectDatabase(Repository repository)
{
    public Tree CreateEmptyTree() => new(repository, Repository.Execute(repository.DirectoryPath, true, null, "", "mktree").Trim());
    public GitObject CreateBlob(Stream content)
    {
        var path = Path.GetTempFileName();
        try
        {
            using (var file = File.Create(path))
            {
                content.CopyTo(file);
            }

            return new(repository, repository.Run("hash-object", "--no-filters", "-w", path).Trim());
        }
        finally { File.Delete(path); }
    }
    public Commit CreateCommit(Signature author, Signature committer, string message, Tree tree, IEnumerable<Commit> parents)
    {
        var args = new List<string> { "commit-tree", tree.Sha };
        foreach (var parent in parents) { args.Add("-p"); args.Add(parent.Sha); }
        args.Add("-m"); args.Add(message);
        return repository.GetCommit(repository.RunWithSignature(author, committer, null, [.. args]).Trim());
    }
}
public sealed class WorktreeCollection(Repository repository)
{
    public void AddNewBranch(string name, string path) => repository.Run("worktree", "add", "-b", name, path);
    public void Add(string branch, string path) => repository.Run("worktree", "add", path, branch.StartsWith("refs/heads/", StringComparison.Ordinal) ? branch["refs/heads/".Length..] : branch);
}
public sealed record TreeEntryChanges(string Path);
public class TreeChanges : List<TreeEntryChanges>;
public sealed class Diff(Repository repository)
{
    public TreeChanges Compare<T>(Tree first, Tree second) where T : TreeChanges
    {
        var result = new TreeChanges();
        result.AddRange(repository.Run("diff", "--name-only", first.Sha, second.Sha).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(path => new TreeEntryChanges(path.Trim())));
        return result;
    }
}
public static class Commands
{
    public static void Checkout(IRepository repository, string revision)
    {
        repository.Run("checkout", "--quiet", revision);
        if (revision.Contains('/') && repository.Branches[revision] is { IsRemote: true } branch)
        {
            repository.Run("symbolic-ref", "HEAD", branch.CanonicalName);
        }
    }
    public static void Checkout(IRepository repository, Branch branch) => Checkout(repository, branch.FriendlyName);
    public static void Checkout(IRepository repository, Commit commit) => Checkout(repository, commit.Sha);
    public static void Stage(IRepository repository, string path) => repository.Run("add", "--", path);
    public static void Fetch(IRepository repository, string remote, IEnumerable<string> refSpecs, FetchOptions? _, string? _1)
    {
        var specifications = refSpecs.ToArray();
        repository.Run(["fetch", remote, .. specifications]);
        if (specifications.Length != 0)
        {
            return;
        }
        // Preserve the fixture's fetch-all-advertised-objects behavior, including
        // PR-only commits that are not reachable through the remote's branch map.
        var extraReferences = repository.Run("ls-remote", "--refs", remote)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('\t')[1])
            .Where(name => !name.StartsWith("refs/heads/", StringComparison.Ordinal) && !name.StartsWith("refs/tags/", StringComparison.Ordinal))
            .ToArray();
        if (extraReferences.Length != 0)
        {
            repository.Run(["fetch", remote, .. extraReferences]);
        }
    }
    public static void Pull(IRepository repository, Signature signature, PullOptions _) => repository.RunWithSignature(signature, signature, null, "pull", "--no-rebase", "--no-edit");
}
