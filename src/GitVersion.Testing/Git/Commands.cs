namespace GitVersion.Testing;

/// <summary>
///     Static façade mirroring the shape of the git library's <c>Commands</c> class so existing
///     test call sites keep compiling against <see cref="TestRepository" />.
/// </summary>
public static class Commands
{
    public static void Checkout(TestRepository repository, string committish) => repository.Checkout(committish);

    public static void Checkout(TestRepository repository, TestBranch branch) => repository.Checkout(branch);

    public static void Checkout(TestRepository repository, TestCommit commit) => repository.Checkout(commit);

    public static void Stage(TestRepository repository, string path) => repository.Stage(path);

    public static void Fetch(TestRepository repository, string remote, IEnumerable<string> refspecs, FetchOptions? options, string? logMessage)
    {
        _ = options;
        _ = logMessage;
        repository.Fetch(remote, refspecs);
    }

    public static void Pull(TestRepository repository, Signature merger, PullOptions? options)
    {
        _ = options;
        repository.Run(merger, merger, ["pull", "--no-edit"]);
    }
}
