namespace GitVersion.Testing;

/// <summary>
///     Worktree management for a <see cref="TestRepository" />.
/// </summary>
public sealed class TestWorktreeCollection(TestRepository repository)
{
    /// <summary>
    ///     Adds a worktree at <paramref name="path" /> checked out at the given branch or committish.
    /// </summary>
    public void Add(string committishOrBranchSpec, string name, string path, bool isLocked)
    {
        _ = name;
        var committish = committishOrBranchSpec.StartsWith("refs/heads/", StringComparison.Ordinal)
            ? committishOrBranchSpec["refs/heads/".Length..]
            : committishOrBranchSpec;
        List<string> arguments = ["worktree", "add"];
        if (isLocked)
        {
            arguments.Add("--lock");
        }

        arguments.Add(path);
        arguments.Add(committish);
        repository.Run([.. arguments]);
    }

    /// <summary>
    ///     Adds a worktree at <paramref name="path" /> on a new branch <paramref name="name" /> created from HEAD.
    /// </summary>
    public void Add(string name, string path, bool isLocked)
    {
        List<string> arguments = ["worktree", "add"];
        if (isLocked)
        {
            arguments.Add("--lock");
        }

        arguments.Add("-b");
        arguments.Add(name);
        arguments.Add(path);
        repository.Run([.. arguments]);
    }
}
